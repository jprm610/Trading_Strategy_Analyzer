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
	public class Heat_Zones : Strategy
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
				Description = @"Enter the description for your new custom Strategy here.";
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
			Instance = 10;
			Heat_zone_strength = 2;
			Width = 1.5; //ATR units
			Heat_zones_print = 6;
            #endregion

            #region Chart_Initialization
            if (BarsInProgress != 0)
				return;

			if (CurrentBars[0] < 0)
				return;

			if (CurrentBar <= BarsRequiredToTrade)
				return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iSwing1.SwingHigh[0] == 0 || iSwing1.SwingLow[0] == 0 ||
				iATR[0] == 0)
				return;

			for (int i = 1; i <= Instance; i++)
			{
				if (iSwing1.SwingHighBar(0, i, CurrentBar) == -1)
					return;

				if (iSwing1.SwingLowBar(0, i, CurrentBar) == -1)
					return;
			}

			heat_zone_swings1.Clear();
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

			#region Reference_Value_Definition
			if (last_swingHigh1 != iSwing1.SwingHigh[0])
				heat_zone_reference_value = swings1_high[0].value;

			if (last_swingLow1 != iSwing1.SwingLow[0])
				heat_zone_reference_value = swings1_low[0].value;
			#endregion

			#region Swing_Heat
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
			if (heat_zone_swings1.Count - 1 >= Heat_zone_strength)
			{
				heat_zones.Add(new MyHeatZone()
				{
					reference_value = heat_zone_reference_value,
					max_y = heat_zone_swings1[0].value,
					min_y = heat_zone_swings1[0].value,
					start_x = heat_zone_swings1[0].bar,
					strength = heat_zone_swings1.Count - 1
				});

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
					}
				}
			}

			while (heat_zones.Count > Heat_zones_print)
            {
				heat_zones.RemoveAt(0);
            }
			#endregion

			#region Extreme_Swings
			max_swing = iSwing1.SwingHigh[0];
			for (int i = 1; i < Instance; i++)
			{
				if (iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] > max_swing)
				{
					max_swing = iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MaxLine", max_swing, Brushes.LimeGreen);

			min_swing = iSwing1.SwingLow[0];
			for (int i = 1; i < Instance; i++)
			{
				if (iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)] < min_swing)
				{
					min_swing = iSwing1.SwingLow[iSwing1.SwingLowBar(0, i, CurrentBar)];
				}
			}
			Draw.HorizontalLine(this, "MinLine", min_swing, Brushes.LimeGreen);
			#endregion

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
			while (swings.Count > Instance)
			{
				swings.RemoveAt(0);
			}
		}

		private void Print_Heat_Zones()
		{
			for (int i = 0; i < heat_zones.Count; i++)
            {
				Draw.Rectangle(this, "Rectangle" + i, CurrentBar - heat_zones[i].start_x, heat_zones[i].min_y, 0, heat_zones[i].max_y, Brushes.Orange);
				Draw.Text(this, "Strength" + i, heat_zones[i].strength.ToString(), 10, (heat_zones[i].max_y + heat_zones[i].min_y) / 2);
				Draw.Text(this, "RV" + i, heat_zones[i].reference_value.ToString(), 40, (heat_zones[i].max_y + heat_zones[i].min_y) / 2);
			}
		}
		#endregion
    }
}
