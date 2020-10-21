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
		private Swing iSwing1, iSwing2, iSwing3;
		private int heat_zone_high_counter, heat_zone_low_counter, heat_zone_high_bar, heat_zone_low_bar, first_swingHigh_heat;
		private double heat_zone_high_value, heat_zone_low_value, max_swing, min_swing, last_swingHigh1, last_swingLow1, swingDis, current_swingHigh2, current_swingLow2, current_swingHigh3, current_swingLow3;
		List<double> heat_zones_high = new List<double>();
		List<double> heat_zones_low = new List<double>();
		//List<double> swings1_high = new List<double>();
		//List<int> swings1_high_bar = new List<int>();
		List<double> swings2_high = new List<double>();
		List<int> swings2_high_bar = new List<int>();
		List<double> swings3_high = new List<double>();
		List<int> swings3_high_bar = new List<int>();
		//List<double> swings1_low = new List<double>();
		//List<int> swings1_low_bar = new List<int>();
		List<double> swings2_low = new List<double>();
		List<int> swings2_low_bar = new List<int>();
		List<double> swings3_low = new List<double>();
		List<int> swings3_low_bar = new List<int>();
		List<MySwing> swings1_high = new List<MySwing>();
		List<MySwing> swings1_low = new List<MySwing>();
		List<MySwing> heat_zone_high_swings1 = new List<MySwing>();
		List<MySwing> heat_zone_low_swings1 = new List<MySwing>();
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

				//iSwing1 > iSwing2 > iSwing3
				iSwing1 = Swing(Close, 36);
				iSwing2 = Swing(Close, 14);
				iSwing3 = Swing(Close, 4);

				//iSMA1 > iSMA2 > iSMA3
				iSMA1 = SMA(Close, 200);
				iSMA2 = SMA(Close, 50);
				iSMA3 = SMA(Close, 20);

				iSMA1.Plots[0].Brush = Brushes.Red;
				iSMA2.Plots[0].Brush = Brushes.Gold;
				iSMA3.Plots[0].Brush = Brushes.Lime;
				iATR.Plots[0].Brush = Brushes.White;
				iSwing1.Plots[0].Brush = Brushes.DarkCyan;
				iSwing1.Plots[1].Brush = Brushes.DarkCyan;
				iSwing2.Plots[0].Brush = Brushes.Silver;
				iSwing2.Plots[1].Brush = Brushes.Silver;
				iSwing3.Plots[0].Brush = Brushes.Magenta;
				iSwing3.Plots[1].Brush = Brushes.Magenta;

				AddChartIndicator(iSMA1);
				AddChartIndicator(iSMA2);
				AddChartIndicator(iSMA3);
				AddChartIndicator(iSwing3);
				AddChartIndicator(iSwing2);
				AddChartIndicator(iSwing1);
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
			if (iSwing3.SwingHigh[0] == 0 || iSwing3.SwingLow[0] == 0 ||
				iSwing2.SwingHigh[0] == 0 || iSwing2.SwingLow[0] == 0 ||
				iSwing1.SwingHigh[0] == 0 || iSwing1.SwingLow[0] == 0 ||
				iATR[0] == 0)
				return;

			for (int i = 1; i <= instance; i++)
			{
				if (iSwing1.SwingHighBar(0, i, CurrentBar) == -1)
					return;

				if (iSwing1.SwingLowBar(0, i, CurrentBar) == -1)
					return;
			}
			#endregion

			#region save_Swings
			if (last_swingHigh1 != iSwing1.SwingHigh[0])
			{
				for (int i = 1; i <= instance; i++)
				{
					swings1_high.Add(new MySwing()
					{
						value = iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)],
						bar = CurrentBar - iSwing1.SwingHighBar(0, i, CurrentBar)
					});
				}
			}
			Lists_Cleaning(swings1_high);

			if (last_swingLow1 != iSwing1.SwingLow[0])
			{
				for (int i = 1; i <= instance; i++)
				{
					swings1_low.Add(new MySwing()
					{
						value = iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)],
						bar = CurrentBar - iSwing1.SwingLowBar(0, i, CurrentBar)
					});
				}
			}
			Lists_Cleaning(swings1_low);
			#endregion

			#region Swing_High_Heat
			if (last_swingLow1 != iSwing1.SwingLow[0])
			{
				heat_zone_high_value = swings1_high[0].value;
				heat_zone_high_counter = 0;
				for (int i = swings1_high.Count - 1; i > 0; i--)
				{
					swingDis = Math.Abs(heat_zone_high_value - swings1_high[i].value);

					if (swingDis <= iATR[0] * 0.5)
					{
						heat_zone_high_swings1.Add(new MySwing()
						{
							value = swings1_high[i].value,
							bar = swings1_high[i].bar
						});
					}
				}

				for (int i = swings1_low.Count - 1; i > 0; i--)
				{
					swingDis = Math.Abs(heat_zone_high_value - swings1_low[i].value);

					if (swingDis <= iATR[0] * 0.5)
					{
						heat_zone_high_swings1.Add(new MySwing()
						{
							value = swings1_low[i].value,
							bar = swings1_low[i].bar
						});
					}
				}
				Lists_Cleaning(heat_zone_high_swings1);
			}
			#endregion

			#region Swing_Low_Heat
			if (last_swingLow1 != iSwing1.SwingLow[0])
			{
				heat_zone_low_value = swings1_low[0].value;
				heat_zone_low_counter = 0;
				for (int i = swings1_high.Count - 1; i > 0; i--)
				{
					swingDis = Math.Abs(heat_zone_low_value - swings1_high[i].value);

					if (swingDis <= iATR[0] * 0.5)
					{
						heat_zone_low_swings1.Add(new MySwing()
						{
							value = swings1_high[i].value,
							bar = swings1_high[i].bar
						});
					}
				}

				for (int i = swings1_low.Count - 1; i > 0; i--)
				{
					swingDis = Math.Abs(heat_zone_low_value - swings1_low[i].value);

					if (swingDis <= iATR[0] * 0.5)
					{
						heat_zone_low_swings1.Add(new MySwing()
						{
							value = swings1_low[i].value,
							bar = swings1_low[i].bar
						});
					}
				}
				Lists_Cleaning(heat_zone_low_swings1);
			}
			#endregion

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

			if (last_swingHigh1 != iSwing1.SwingHigh[0] || last_swingLow1 != iSwing1.SwingLow[0])
				Print_Heat_Zones();

			last_swingHigh1 = iSwing1.SwingHigh[0];
			last_swingLow1 = iSwing1.SwingLow[0];
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

		public class MySwing
		{
			public double value;
			public int bar;
		}

		private void Lists_Cleaning(List<MySwing> swings)
		{
			/*
            for (int i = 0; i < swings_bar.Count; i++)
			{
				for (int j = 0; j < swings_bar.Count; j++)
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
			*/

			while (swings.Count > 10)
			{
				swings.RemoveAt(0);
			}
		}

		private void Lists_Length(List<double> swings, List<int> swings_bar)
		{
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
			Print("swingsHigh");
			for (int i = 0; i < swings1_high.Count; i++)
			{
				Print(string.Format("{0} // {1}", swings1_high[i].value, swings1_high[i].bar));
			}
			Print("-----");

			Print("heat_zone_high");
			Print(heat_zone_high_value);
			for (int i = 0; i < heat_zone_high_swings1.Count; i++)
			{
				Print(string.Format("{0} // {1}", heat_zone_high_swings1[i].value, heat_zone_high_swings1[i].bar));
			}
			Print("-----");

			/*
			Print("swingsHigh2");
			for (int i = 0; i < swings2_high.Count; i++)
			{
				Print(string.Format("{0} // {1}", swings2_high[i], swings2_high_bar[i]));
			}
			Print("-----");

			Print("swingsHigh3");
			for (int i = 0; i < swings3_high.Count; i++)
			{
				Print(string.Format("{0} // {1}", swings3_high[i], swings3_high_bar[i]));
			}
			Print("-----");
			*/
			Print("swingsLow");
			for (int i = 0; i < swings1_low.Count; i++)
			{
				Print(string.Format("{0} // {1}", swings1_low[i].value, swings1_low[i].bar));
			}
			Print("-----");

			Print("heat_zone_low");
			Print(heat_zone_low_value);
			for (int i = 0; i < heat_zone_low_swings1.Count; i++)
			{
				Print(string.Format("{0} // {1}", heat_zone_low_swings1[i].value, heat_zone_low_swings1[i].bar));
			}
			//Print("-----");

			/*
			Print("swingsLow2");
			for (int i = 0; i < swings2_low.Count; i++)
			{
				Print(string.Format("{0} // {1}", swings2_low[i], swings2_low_bar[i]));
			}
			Print("-----");

			Print("swingsLow3");
			for (int i = 0; i < swings3_low.Count; i++)
			{
				Print(string.Format("{0} // {1}", swings3_low[i], swings3_low_bar[i]));
			}*/

			Print(string.Format("LowCount: {0} // HighCount: {1}", heat_zone_low_swings1.Count, heat_zone_high_swings1.Count));
			Print("---------------------------------------------------------------------------------------");

			heat_zone_high_swings1.Clear();
			heat_zone_low_swings1.Clear();
		}
		#endregion
    }
}
