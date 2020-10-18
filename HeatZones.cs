#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class HeatZones : Strategy
	{
		#region Variables
		private SMA iSMA1, iSMA2, iSMA3;
		private ATR iATR;
		private Swing iSwing1, iSwing2;
		private int heat_zone_high_counter, heat_zone_low_counter, heat_zone_high_bar, heat_zone_low_bar, a;
		private double heat_zone_high_value, heat_zone_low_value, max_swing, min_swing, current_swingHigh2, current_swingLow2;
		List<double> heat_zones_high = new List<double>();
		List<double> heat_zones_low = new List<double>();
		List<double> heat_zones_high_swings1 = new List<double>();
		List<int> heat_zones_high_swings1_bar = new List<int>();
		List<double> heat_zones_high_swings2 = new List<double>();
		List<int> heat_zones_high_swings2_bar = new List<int>();
		List<double> heat_zones_low_swings1 = new List<double>();
		List<int> heat_zones_low_swings1_bar = new List<int>();
		List<double> heat_zones_low_swings2 = new List<double>();
		List<int> heat_zones_low_swings2_bar = new List<int>();
		List<double> heat_zones_high_swings1_c = new List<double>();
		List<int> heat_zones_high_swings1_bar_c = new List<int>();
		List<double> heat_zones_high_swings2_c = new List<double>();
		List<int> heat_zones_high_swings2_bar_c = new List<int>();
		List<double> heat_zones_low_swings1_c = new List<double>();
		List<int> heat_zones_low_swings1_bar_c = new List<int>();
		List<double> heat_zones_low_swings2_c = new List<double>();
		List<int> heat_zones_low_swings2_bar_c  = new List<int>();
		List<int> heat_zones_bar_high = new List<int>();
		List<int> heat_zones_bar_low = new List<int>();
		#endregion
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Enter the description for your new custom Strategy here.";
				Name = "Heat_Zones";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.Infinite;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;
				
				#region Parameters
				instance = 10;
				heat_zone_strength = 10;
				#endregion
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				iATR = ATR(Close, 100);
				iSwing1 = Swing(Close, 4);
				iSwing2 = Swing(Close, 14);
				iSMA1 = SMA(Close, 200);
				iSMA2 = SMA(Close, 50);
				iSMA3 = SMA(Close, 20);

				iSMA1.Plots[0].Brush = Brushes.Red;
				iSMA2.Plots[0].Brush = Brushes.Gold;
				iSMA3.Plots[0].Brush = Brushes.Lime;
				iATR.Plots[0].Brush = Brushes.White;
				iSwing1.Plots[0].Brush = Brushes.Fuchsia;
				iSwing1.Plots[1].Brush = Brushes.Gold;
				iSwing2.Plots[0].Brush = Brushes.Silver;
				iSwing2.Plots[1].Brush = Brushes.Silver;

				AddChartIndicator(iSMA1);
				AddChartIndicator(iSMA2);
				AddChartIndicator(iSMA3);
				AddChartIndicator(iSwing1);
				AddChartIndicator(iSwing2);
			}
		}

		protected override void OnBarUpdate()
		{
			#region Chart_Initialization
			if (BarsInProgress != 0)
				return;

			if (CurrentBars[0] < 0)
				return;

			if (CurrentBar <= BarsRequiredToTrade)
				return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iSwing2.SwingHigh[0] == 0 || iSwing2.SwingLow[0] == 0 || iSwing1.SwingHigh[0] == 0 || iSwing1.SwingLow[0] == 0 || iATR[0] == 0)
				return;
			
			for (int i = 1; i <= (instance * 3); i++)
            {
				if (iSwing1.SwingHighBar(0, i, CurrentBar) == -1 ||
					iSwing2.SwingHighBar(0, i, CurrentBar) == -1)
				{
					return;
				}

				if (iSwing1.SwingLowBar(0, i, CurrentBar) == -1 ||
					iSwing2.SwingLowBar(0, i, CurrentBar) == -1)
				{
					return;
				}
			}
			#endregion

			/*
			if (current_swingHigh2 == iSwing2.SwingHigh[0] ||
				current_swingLow2 == iSwing2.SwingLow[0])
            {
				return;
            }
			*/

			current_swingHigh2 = iSwing2.SwingHigh[0];
			current_swingLow2 = iSwing2.SwingLow[0];

            #region Swing_High_Heat
            heat_zone_high_value = iSwing2.SwingHigh[0];
			heat_zone_high_bar = iSwing2.SwingHighBar(0, 1, CurrentBar);
			heat_zone_high_counter = 0;

			#region SwingHigh2
			for (int i = 1; i <= instance; i++)
			{
				if (iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] < heat_zone_high_value + (iATR[0] * 0.5) &&
					iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] > heat_zone_high_value - (iATR[0] * 0.5))
				{
					heat_zones_high_swings2.Add(iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)]);
					heat_zones_high_swings2_bar.Add(CurrentBar - iSwing2.SwingHighBar(0, i, CurrentBar));
				}

				if (iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)] < heat_zone_high_value + (iATR[0] * 0.5) &&
					iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)] > heat_zone_high_value - (iATR[0] * 0.5))
				{
					heat_zones_high_swings2.Add(iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)]);
					heat_zones_high_swings2_bar.Add(CurrentBar - iSwing2.SwingLowBar(0, i, CurrentBar));
				}
			}
			Lists_Cleaning(heat_zones_high_swings2, heat_zones_high_swings2_bar);
            #endregion

            #region swingHigh1
            for (int i = 1; i <= (instance * 3); i++)
            {
				if (iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] < heat_zone_high_value + (iATR[0] * 0.5) &&
					iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] > heat_zone_high_value - (iATR[0] * 0.5))
				{
					heat_zones_high_swings1.Add(iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)]);
					heat_zones_high_swings1_bar.Add(CurrentBar - iSwing1.SwingHighBar(0, i, CurrentBar));
				}

				if (iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)] < heat_zone_high_value + (iATR[0] * 0.5) &&
					iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)] > heat_zone_high_value - (iATR[0] * 0.5))
				{
					heat_zones_high_swings1.Add(iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)]);
					heat_zones_high_swings1_bar.Add(CurrentBar - iSwing1.SwingLowBar(0, i, CurrentBar));
				}
			}
			Lists_Cleaning(heat_zones_high_swings1, heat_zones_high_swings1_bar);
			#endregion
			#endregion

			#region Swing_Low_Heat

			heat_zone_low_value = iSwing2.SwingLow[0];
			heat_zone_low_bar = iSwing2.SwingLowBar(0, 1, CurrentBar);
			heat_zone_low_counter = 0;

            #region swingLow2
            for (int i = 1; i <= instance; i++)
			{				
				if (iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] < heat_zone_low_value + (iATR[0] * 0.5) &&
					iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] > heat_zone_low_value - (iATR[0] * 0.5))
				{
					heat_zones_low_swings2.Add(iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)]);
					heat_zones_low_swings2_bar.Add(CurrentBar - iSwing2.SwingHighBar(0, i, CurrentBar));
				}				

				if (iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)] < heat_zone_low_value + (iATR[0] * 0.5) &&
					iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)] > heat_zone_low_value - (iATR[0] * 0.5))
				{
					heat_zones_low_swings2.Add(iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)]);
					heat_zones_low_swings2_bar.Add(CurrentBar - iSwing2.SwingLowBar(0, i, CurrentBar));
				}
			}
			Lists_Cleaning(heat_zones_low_swings2, heat_zones_low_swings2_bar);
			#endregion

			#region swingLow1
			for (int i = 1; i <= (instance * 3); i++)
            {
				if (iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] < heat_zone_low_value + (iATR[0] * 0.5) &&
					iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] > heat_zone_low_value - (iATR[0] * 0.5))
				{
					heat_zones_low_swings1.Add(iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)]);
					heat_zones_low_swings1_bar.Add(CurrentBar - iSwing1.SwingHighBar(0, i, CurrentBar));
				}

				if (iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)] < heat_zone_low_value + (iATR[0] * 0.5) &&
					iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)] > heat_zone_low_value - (iATR[0] * 0.5))
				{
					heat_zones_low_swings1.Add(iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)]);
					heat_zones_low_swings1_bar.Add(CurrentBar - iSwing1.SwingLowBar(0, i, CurrentBar));
				}
			}
			Lists_Cleaning(heat_zones_low_swings1, heat_zones_low_swings1_bar);
			#endregion
			#endregion

			/*
			Lists_Cleaning(heat_zones_high_swings2, heat_zones_high_swings2_bar, heat_zones_high_swings1, heat_zones_high_swings1_bar);
			Lists_Cleaning(heat_zones_low_swings2, heat_zones_low_swings2_bar, heat_zones_low_swings1, heat_zones_low_swings1_bar);
			

			if (heat_zones_high_swings2_c.Count > 10)
			{
				heat_zones_high_swings2_c.RemoveAt(0);
				heat_zones_high_swings2_bar_c.RemoveAt(0);
			}

			if (heat_zones_high_swings1_c.Count > 10)
			{
				heat_zones_high_swings1_c.RemoveAt(0);
				heat_zones_high_swings1_bar_c.RemoveAt(0);
			}

			if (heat_zones_low_swings1_c.Count > 10)
			{
				heat_zones_low_swings1_c.RemoveAt(0);
				heat_zones_low_swings1_bar_c.RemoveAt(0);
			}

			if (heat_zones_low_swings2_c.Count > 10)
			{
				heat_zones_low_swings2_c.RemoveAt(0);
				heat_zones_low_swings2_bar_c.RemoveAt(0);
			}

			
			Lists_Length(heat_zones_low_swings1, heat_zones_low_swings1_bar);
			Lists_Length(heat_zones_low_swings2, heat_zones_low_swings2_bar);
			Lists_Length(heat_zones_high_swings1, heat_zones_high_swings1_bar);
			Lists_Length(heat_zones_high_swings2, heat_zones_high_swings2_bar);
			*/
			#region Extreme_Swings
			max_swing = iSwing2.SwingHigh[0];
			for (int i = 1; i < instance; i++)
            {
				if (iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] > max_swing)
                {
					max_swing = iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MaxLine", max_swing, Brushes.Yellow);
			
			min_swing = iSwing2.SwingLow[0];
			for (int i = 1; i < instance; i++)
			{
				if (iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)] < min_swing)
				{
					min_swing = iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MinLine", min_swing, Brushes.Yellow);
			#endregion

			Print_Heat_Zones();
		}
		
		#region Parameters
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Instance", Order = 1, GroupName = "Parameters")]
		public int instance
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Heat-Zone Strength", Order = 1, GroupName = "Parameters")]
		public int heat_zone_strength
		{ get; set; }
		#endregion

		#region Functions
		private void Lists_Cleaning(List<double> swings, List<int> swings_bar)
		{
            for (int i = 0; i < swings.Count; i++)
			{
				for (int j = 0; j < swings.Count; j++)
				{
					if (i != j)
                    {
						if (swings_bar[i] == swings_bar[j])
                        {
							swings.RemoveAt(i);
							swings_bar.RemoveAt(i);
                        }
                    }
				}
			}

			if (swings.Count > 10)
			{
				swings.RemoveAt(0);
				swings_bar.RemoveAt(0);
			}
		}

		private void Print_Heat_Zones()
        {
			/*
			Print("High");
			for (int i = 0; i < heat_zones_high.Count; i++)
            {
				Print(string.Format("{0} // {1} // {2}", Time[0], heat_zones_high[i], heat_zones_bar_high[i]));
				//Draw.HorizontalLine(this, "Line" + i, heat_zones_high[i], Brushes.Red);
				//Draw.RegionHighlightY(this, "Range" + i, heat_zones_high[i] - (iATR[0] * 0.5), heat_zones_high[i] + (iATR[0] * 0.5), Brushes.Red);
				Draw.Line(this, "Line(H)" + i, CurrentBar - heat_zones_bar_high[i], heat_zones_high[i], 0, heat_zones_high[i], Brushes.Red);
				Draw.Rectangle(this, "Rectangle(H)" + i, CurrentBar - heat_zones_bar_high[i], heat_zones_high[i] - (iATR[0] * 0.5), 0, heat_zones_high[i] + (iATR[0] * 0.5), Brushes.Red);
			}
			Print("-----");
			Print("Low");
			for (int i = 0; i < heat_zones_low.Count; i++)
			{
				Print(string.Format("{0} // {1} // {2}", Time[0], heat_zones_low[i], heat_zones_bar_low[i]));
				//Draw.HorizontalLine(this, "Line" + i, heat_zones_low[i], Brushes.Red);
				//Draw.RegionHighlightY(this, "Range" + i, heat_zones_low[i] - (iATR[0] * 0.5), heat_zones_low[i] + (iATR[0] * 0.5), Brushes.Red);
				Draw.Line(this, "Line(L)" + i, CurrentBar - heat_zones_bar_low[i], heat_zones_low[i], 0, heat_zones_low[i], Brushes.Yellow);
				Draw.Rectangle(this, "Rectangle(L)" + i, CurrentBar - heat_zones_bar_low[i], heat_zones_low[i] - (iATR[0] * 0.5), 0, heat_zones_low[i] + (iATR[0] * 0.5), Brushes.Yellow);
			}
			Print("-----");
			*/

			Print(Time[0]);
			Print("heat_zones_high_swings1");
			for (int i = 0; i < heat_zones_high_swings1_c.Count; i++)
            {
				Print(string.Format("{0} // {1}", heat_zones_high_swings1_c[i], heat_zones_high_swings1_bar_c[i]));
            }
			Print("-----");

			Print("heat_zones_high_swings2");
			for (int i = 0; i < heat_zones_high_swings2_c.Count; i++)
			{
				Print(string.Format("{0} // {1}", heat_zones_high_swings2_c[i], heat_zones_high_swings2_bar_c[i]));
			}
			Print("-----");

			Print("heat_zones_low_swings1");
			for (int i = 0; i < heat_zones_low_swings1_c.Count; i++)
			{
				Print(string.Format("{0} // {1}", heat_zones_low_swings1_c[i], heat_zones_low_swings1_bar_c[i]));
			}
			Print("-----");

			Print("heat_zones_low_swings2");
			for (int i = 0; i < heat_zones_low_swings2_c.Count; i++)
			{
				Print(string.Format("{0} // {1}", heat_zones_low_swings2_c[i], heat_zones_low_swings2_bar_c[i]));
			}
			Print("-----");
		}
		#endregion
    }
}
