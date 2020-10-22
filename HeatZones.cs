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
		private int Instance, Heat_zone_strength, Heat_zones_print;
		private double Width, heat_zone_high_value, heat_zone_low_value, max_swing, min_swing, last_swingHigh1, last_swingLow1, swingDis, current_swingHigh2, current_swingLow2, current_swingHigh3, current_swingLow3;
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
		List<MyHeatZone> heat_zones_high = new List<MyHeatZone>();
		List<MyHeatZone> heat_zones_low = new List<MyHeatZone>();
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
			#region Parameters
			Instance = 10;
			Heat_zone_strength = 2;
			Width = 1.5; //ATR units
			Heat_zones_print = 3;
            #endregion

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

			for (int i = 1; i <= Instance; i++)
			{
				if (iSwing1.SwingHighBar(0, i, CurrentBar) == -1)
					return;

				if (iSwing1.SwingLowBar(0, i, CurrentBar) == -1)
					return;
			}

			heat_zone_high_swings1.Clear();
			heat_zone_low_swings1.Clear();
			#endregion

			#region Save_Swings
			if (last_swingHigh1 != iSwing1.SwingHigh[0] || last_swingLow1 != iSwing1.SwingLow[0])
			{
				for (int i = 1; i <= Instance; i++)
				{
					swings1_high.Add(new MySwing()
					{
						value = iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)],
						bar = CurrentBar - iSwing1.SwingHighBar(0, i, CurrentBar)
					});
				}
				Lists_Cleaning(swings1_high);

				for (int i = 1; i <= Instance; i++)
				{
					swings1_low.Add(new MySwing()
					{
						value = iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)],
						bar = CurrentBar - iSwing1.SwingLowBar(0, i, CurrentBar)
					});
				}
				Lists_Cleaning(swings1_low);
			}
			#endregion

			#region Swing_High_Heat
			if (last_swingHigh1 != iSwing1.SwingHigh[0] || last_swingLow1 != iSwing1.SwingLow[0])
			{
				heat_zone_high_value = swings1_high[0].value;
				for (int i = 0; i < swings1_high.Count - 1; i++)
				{
					swingDis = Math.Abs(heat_zone_high_value - swings1_high[i].value);

					if (swingDis <= iATR[0] * Width)
					{
						heat_zone_high_swings1.Add(new MySwing()
						{
							value = swings1_high[i].value,
							bar = swings1_high[i].bar
						});
					}
				}

				for (int i = 0; i < swings1_low.Count - 1; i++)
				{
					swingDis = Math.Abs(heat_zone_high_value - swings1_low[i].value);

					if (swingDis <= iATR[0] * Width)
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
			if (last_swingHigh1 != iSwing1.SwingHigh[0] || last_swingLow1 != iSwing1.SwingLow[0])
			{
				heat_zone_low_value = swings1_low[0].value;
				for (int i = 0; i < swings1_high.Count - 1; i++)
				{
					swingDis = Math.Abs(heat_zone_low_value - swings1_high[i].value);

					if (swingDis <= iATR[0] * Width)
					{
						heat_zone_low_swings1.Add(new MySwing()
						{
							value = swings1_high[i].value,
							bar = swings1_high[i].bar
						});
					}
				}

				for (int i = 0; i < swings1_low.Count - 1; i++)
				{
					swingDis = Math.Abs(heat_zone_low_value - swings1_low[i].value);

					if (swingDis <= iATR[0] * Width)
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

			#region Heat_Zone_Identification
			#region Heat_Zone_High
			if (heat_zone_high_swings1.Count - 1 >= Heat_zone_strength)
			{
				heat_zones_high.Add(new MyHeatZone()
				{
					reference_value = heat_zone_high_value,
					max_y = heat_zone_high_swings1[0].value,
					min_y = heat_zone_high_swings1[0].value,
					start_x = heat_zone_high_swings1[0].bar,
					strength = heat_zone_high_swings1.Count - 1
				});

				for (int i = 0; i < heat_zone_high_swings1.Count; i++)
				{
					if (heat_zone_high_swings1[i].value > heat_zones_high[heat_zones_high.Count - 1].max_y)
					{
						heat_zones_high[heat_zones_high.Count - 1].max_y = heat_zone_high_swings1[i].value;
					}

					if (heat_zone_high_swings1[i].value < heat_zones_high[heat_zones_high.Count - 1].min_y)
					{
						heat_zones_high[heat_zones_high.Count - 1].min_y = heat_zone_high_swings1[i].value;
					}

					if (heat_zone_high_swings1[i].bar < heat_zones_high[heat_zones_high.Count - 1].start_x)
					{
						heat_zones_high[heat_zones_high.Count - 1].start_x = heat_zone_high_swings1[i].bar;
					}
				}
			}

			for (int i = 0; i < heat_zones_high.Count; i++)
			{
				for (int j = 0; j < heat_zones_high.Count; j++)
				{
					if (i != j)
					{
						if (heat_zones_high[i].start_x == heat_zones_high[j].start_x)
						{
							heat_zones_high.RemoveAt(i);
						}
					}
				}
			}

			while (heat_zones_high.Count > Heat_zones_print)
            {
				heat_zones_high.RemoveAt(0);
            }
            #endregion

            #region Heat_Zone_Low
            if (heat_zone_low_swings1.Count - 1 >= Heat_zone_strength)
			{
				heat_zones_low.Add(new MyHeatZone()
				{
					reference_value = heat_zone_low_value,
					max_y = heat_zone_low_swings1[0].value,
					min_y = heat_zone_low_swings1[0].value,
					start_x = heat_zone_low_swings1[0].bar,
					strength = heat_zone_low_swings1.Count - 1
				});

				for (int i = 0; i < heat_zone_low_swings1.Count; i++)
				{
					if (heat_zone_low_swings1[i].value > heat_zones_low[heat_zones_low.Count - 1].max_y)
					{
						heat_zones_low[heat_zones_low.Count - 1].max_y = heat_zone_low_swings1[i].value;
					}

					if (heat_zone_low_swings1[i].value < heat_zones_low[heat_zones_low.Count - 1].min_y)
					{
						heat_zones_low[heat_zones_low.Count - 1].min_y = heat_zone_low_swings1[i].value;
					}

					if (heat_zone_low_swings1[i].bar < heat_zones_low[heat_zones_low.Count - 1].start_x)
					{
						heat_zones_low[heat_zones_low.Count - 1].start_x = heat_zone_low_swings1[i].bar;
					}
				}
			}

			for (int i = 0; i < heat_zones_low.Count; i++)
			{
				for (int j = 0; j < heat_zones_low.Count; j++)
				{
					if (i != j)
					{
						if (heat_zones_low[i].start_x == heat_zones_low[j].start_x)
						{
							heat_zones_low.RemoveAt(i);
						}
					}
				}
			}

			while (heat_zones_low.Count > Heat_zones_print)
            {
				heat_zones_low.RemoveAt(0);
            }
			#endregion
			#endregion

			#region Extreme_Swings
			max_swing = iSwing2.SwingHigh[0];
			for (int i = 1; i < Instance; i++)
			{
				if (iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] > max_swing)
				{
					max_swing = iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MaxLine", max_swing, Brushes.LimeGreen);

			min_swing = iSwing2.SwingLow[0];
			for (int i = 1; i < Instance; i++)
			{
				if (iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)] < min_swing)
				{
					min_swing = iSwing2.SwingLow[iSwing2.SwingLowBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MinLine", min_swing, Brushes.LimeGreen);
			#endregion

			if (last_swingHigh1 != iSwing1.SwingHigh[0] || last_swingLow1 != iSwing1.SwingLow[0])
				Print_Heat_Zones();

			last_swingHigh1 = iSwing1.SwingHigh[0];
			last_swingLow1 = iSwing1.SwingLow[0];
		}

		#region Functions

		public class MySwing
		{
			public double value;
			public int bar;
		}

		public class MyHeatZone
        {
			public double reference_value;
			public double max_y;
			public double min_y;
			public int start_x;
			public int strength;
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
			Print(Time[0]);

			/*
			Print(string.Format("High: {0} // Low: {1}", heat_zones_high.Count, heat_zones_low.Count));

			Print("High");
			for (int i = 0; i < heat_zones_high.Count; i++)
			{
				Print(string.Format("RV: {0}", heat_zones_high[i].reference_value));
			}
			Print("-----");
			Print("Low");
			for (int i = 0; i < heat_zones_low.Count; i++)
			{
				Print(string.Format("RV: {0}", heat_zones_low[i].reference_value));
			}
			Print("-----------------------------------------------------------------");
			*/

			
			Print("High");
			for (int i = 0; i < heat_zones_high.Count; i++)
            {
				Print(string.Format("RV: {0} // MinY: {1} // MaxY: {2} // StartX: {3} // Strength: {4}", heat_zones_high[i].reference_value, heat_zones_high[i].min_y, heat_zones_high[i].max_y, heat_zones_high[i].start_x, heat_zones_high[i].strength));
				Draw.Rectangle(this, "Rectangle(H)" + i, CurrentBar - heat_zones_high[i].start_x, heat_zones_high[i].min_y, 0, heat_zones_high[i].max_y, Brushes.Red);
				Draw.Text(this, "Strength[H]" + i, heat_zones_high[i].strength.ToString(), 10, (heat_zones_high[i].max_y + heat_zones_high[i].min_y) / 2);
			}
			Print("-----");
			Print("Low");
			for (int i = 0; i < heat_zones_low.Count; i++)
			{
				Print(string.Format("RV: {0} // MinY: {1} // MaxY: {2} // StartX: {3} // Strength: {4}", heat_zones_low[i].reference_value, heat_zones_low[i].min_y, heat_zones_low[i].max_y, heat_zones_low[i].start_x, heat_zones_low[i].strength));
				Draw.Rectangle(this, "Rectangle(L)" + i, CurrentBar - heat_zones_low[i].start_x, heat_zones_low[i].min_y, 0, heat_zones_low[i].max_y, Brushes.Yellow);
				Draw.Text(this, "Strength[L]" + i, heat_zones_low[i].strength.ToString(), 10, (heat_zones_low[i].max_y + heat_zones_low[i].min_y) / 2);
			}
			Print("-----------------------------------------------------------------");
			

			/*
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
			}

			Print(string.Format("LowCount: {0} // HighCount: {1}", heat_zone_low_swings1.Count - 1, heat_zone_high_swings1.Count - 1));
			
			Print("---------------------------------------------------------------------------------------");
			*/
		}
		#endregion
    }
}
