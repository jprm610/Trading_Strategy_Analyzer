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
		private ATR iATR;
		private Swing iSwing1;
		private int Instance, Heat_zone_strength, Heat_zones_print;
		private double Width, heat_zone_reference_value, swingDis, last_swingHigh1, last_swingLow1, max_swing, min_swing;
		List<MySwing> swings1_high = new List<MySwing>();
		List<MySwing> swings1_low = new List<MySwing>();
		List<MySwing> heat_zone_swings1 = new List<MySwing>();
		List<MyHeatZone> heat_zones = new List<MyHeatZone>();
		#endregion
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Detects price ranges in where a determined number of swings are confluent.";
				Name = "HeatZones";
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

				iSwing1 = Swing(Close, 36);

				iATR.Plots[0].Brush = Brushes.White;
				iSwing1.Plots[0].Brush = Brushes.DarkCyan;
				iSwing1.Plots[1].Brush = Brushes.DarkCyan;

				AddChartIndicator(iSwing1);
				AddChartIndicator(iATR);
			}
		}

		protected override void OnBarUpdate()
		{
			#region Parameters
			Instance = 10;			//Represents the numeber of swings that are going to be taken into account for later calculations. 
			Heat_zone_strength = 2; //How many swings have to be in the price range to be considered as a Heat Zone?
			Width = 1.5;			//The width of the range that is going to be evaluated as a Heat Zone. (Measured in ATR units).
			Heat_zones_print = 6;	//How many Heat Zones are going to be printed?
            #endregion

            #region Chart_Initialization
            if (BarsInProgress != 0)
				return;

			if (CurrentBars[0] < 0)
				return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iSwing1.SwingHigh[0] == 0 || iSwing1.SwingLow[0] == 0 ||
				iATR[0] == 0)
				return;

			//Review that the swings are charged to avoid later bugs. In other words, to check if the needed swing instances exist.
			for (int i = 1; i <= Instance; i++)
			{
				if (iSwing1.SwingHighBar(0, i, CurrentBar) == -1)
					return;

				if (iSwing1.SwingLowBar(0, i, CurrentBar) == -1)
					return;
			}

			//Reset the swings that are going to be evaluated in each Heat Zone claculation.
			//This has to be done because every Heat Zone has different Reference Values.
			heat_zone_swings1.Clear();
			#endregion

			#region Save_Swings
			//In each swing update, save the last n swings highs and lows, whit its corresponding CurrentBar. (n = Instance)
			//This swings are saved in a list (swings1_high and swings1_low) of class MySwing which is defined in the classes region.
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

			#region Reference_Value_Definition
			//Determine the reference current Heat Zone value in each swing update.
			//If a swingHigh appears, it is going to be taken as the current reference value. The same happens with swings low.
			if (last_swingHigh1 != iSwing1.SwingHigh[0])
				heat_zone_reference_value = swings1_high[0].value;

			if (last_swingLow1 != iSwing1.SwingLow[0])
				heat_zone_reference_value = swings1_low[0].value;
			#endregion

			#region Swing_Heat
			//In each swing update, determine which of the saved swings are located in the current Heat Zone range.
			//If the swing (low or high) is in range, its value and CurrentBar is saved in another list (heat_zone_swings1) of class MySwing.
			if (last_swingHigh1 != iSwing1.SwingHigh[0] || last_swingLow1 != iSwing1.SwingLow[0])
			{
				for (int i = 0; i < swings1_high.Count - 1; i++)
				{
					swingDis = Math.Abs(heat_zone_reference_value - swings1_high[i].value);

					if (swingDis <= iATR[0] * Width)
					{
						heat_zone_swings1.Add(new MySwing()
						{
							value = swings1_high[i].value,
							bar = swings1_high[i].bar
						});
					}
				}

				for (int i = 0; i < swings1_low.Count - 1; i++)
				{
					swingDis = Math.Abs(heat_zone_reference_value - swings1_low[i].value);

					if (swingDis <= iATR[0] * Width)
					{
						heat_zone_swings1.Add(new MySwing()
						{
							value = swings1_low[i].value,
							bar = swings1_low[i].bar
						});
					}
				}
				Lists_Cleaning(heat_zone_swings1);
			}
			#endregion

			#region Heat_Zone_Identification
			//Deetermine if the current Heat Zone satisfies the heat_zone_strength requirement.
			//In other words, save the current Heat Zone (in a list of class MyHeatZone),
			//if it has the same or more number of swings in the range.
			if (heat_zone_swings1.Count - 1 >= Heat_zone_strength)
			{
				//Initialize Heat Zones characteristics.
				//max_y, min_y and start_x are going to be established correctly in the next for loop.
				heat_zones.Add(new MyHeatZone()
				{
					reference_value = heat_zone_reference_value,
					max_y = heat_zone_swings1[0].value,
					min_y = heat_zone_swings1[0].value,
					start_x = heat_zone_swings1[0].bar,
					strength = heat_zone_swings1.Count - 1
				});

				//Determine the dimensions of the rectangle that will be printed as a Heat Zone. 
				for (int i = 0; i < heat_zone_swings1.Count; i++)
				{
					if (heat_zone_swings1[i].value > heat_zones[heat_zones.Count - 1].max_y)
					{
						heat_zones[heat_zones.Count - 1].max_y = heat_zone_swings1[i].value;
					}

					if (heat_zone_swings1[i].value < heat_zones[heat_zones.Count - 1].min_y)
					{
						heat_zones[heat_zones.Count - 1].min_y = heat_zone_swings1[i].value;
					}

					if (heat_zone_swings1[i].bar < heat_zones[heat_zones.Count - 1].start_x)
					{
						heat_zones[heat_zones.Count - 1].start_x = heat_zone_swings1[i].bar;
					}
				}
			}

			//Avoid that a Heat Zone is printed over another one.
			//Evaluating if its Start_x or its reference value is the same. 
			for (int i = 0; i < heat_zones.Count; i++)
			{
				for (int j = 0; j < heat_zones.Count; j++)
				{
					if (i != j)
					{
						if (heat_zones[i].start_x == heat_zones[j].start_x)
						{
							heat_zones.RemoveAt(i);
						}
						//If its reference value is the same. Erase the oldest of both Heat Zones.
						else if (heat_zones[i].reference_value == heat_zones[j].reference_value)
                        {
							if (heat_zones[i].start_x < heat_zones[j].start_x)
                            {
								heat_zones.RemoveAt(i);
                            }
                            else
                            {
								heat_zones.RemoveAt(j);
                            }
                        }
					}
				}
			}

			//Erase old Heat Zones.
			while (heat_zones.Count > Heat_zones_print)
            {
				heat_zones.RemoveAt(0);
            }
			#endregion

			#region Extreme_Swings
			//Determine the extreme swings of the last n Instances.
			//Then plot lines at that values.

			max_swing = iSwing1.SwingHigh[0];
			for (int i = 1; i < Instance; i++)
			{
				if (iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] > max_swing)
				{
					max_swing = iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MaxLine", max_swing, Brushes.LimeGreen, DashStyleHelper.Solid, 5);

			min_swing = iSwing1.SwingLow[0];
			for (int i = 1; i < Instance; i++)
			{
				if (iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)] < min_swing)
				{
					min_swing = iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MinLine", min_swing, Brushes.LimeGreen, DashStyleHelper.Solid, 5);
			#endregion

			Print_Heat_Zones();

			//Reset the last swing value in order to execute the swing update processes.
			last_swingHigh1 = iSwing1.SwingHigh[0];
			last_swingLow1 = iSwing1.SwingLow[0];
		}

        #region Classes
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
        #endregion

        #region Functions
		//Remove the oldest swings.
        private void Lists_Cleaning(List<MySwing> swings)
		{
			while (swings.Count > Instance)
			{
				swings.RemoveAt(0);
			}
		}

		//Prints rectangles that represents the Heat Zones, the strength ot the Heat Zone and its reference value.
		//The rectangle dimenssions were set in the Heat_Zone_Identification region.
		private void Print_Heat_Zones()
		{
			for (int i = 0; i < heat_zones.Count; i++)
            {
				Draw.Rectangle(this, "Rectangle" + i, CurrentBar - heat_zones[i].start_x, heat_zones[i].min_y, 0, heat_zones[i].max_y, Brushes.Orange);
				Draw.Text(this, "Strength" + i, heat_zones[i].strength.ToString(), 10, (heat_zones[i].max_y + heat_zones[i].min_y) / 2);
				Draw.Text(this, "RV" + i, heat_zones[i].reference_value.ToString(), 80, (heat_zones[i].max_y + heat_zones[i].min_y) / 2);
			}
		}
		#endregion
    }
}
