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
		#region
		private SMA iSMA1, iSMA2, iSMA3;
		private ATR iATR;
		private Swing iSwing1, iSwing2;
		private int heat_zone_high_counter, heat_zone_low_counter;
		private double heat_zone_high_value, heat_zone_low_value;
		private bool is_heat_zone_high, is_heat_zone_low;
		List<double> heat_zones = new List<double>();
		List<int> heat_zones_bar = new List<int>();
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
			#endregion

			#region Swing_High
			heat_zone_high_value = iSwing2.SwingHigh[0];
			heat_zone_high_counter = 0;

			for (int i = 1; i < 7; i++)
			{
				if (iSwing1.SwingHighBar(0, i, CurrentBar) == -1 ||
					iSwing2.SwingHighBar(0, i, CurrentBar) == -1)
                {
					return;
                }

				if (iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] < heat_zone_high_value + (iATR[0] * 0.5) &&
					iSwing1.SwingHigh[iSwing1.SwingHighBar(0, i, CurrentBar)] > heat_zone_high_value - (iATR[0] * 0.5))
				{
					heat_zone_high_counter++;
				}

				if (iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] < heat_zone_high_value + (iATR[0] * 0.5) &&
					iSwing2.SwingHigh[iSwing2.SwingHighBar(0, i, CurrentBar)] > heat_zone_high_value - (iATR[0] * 0.5))
				{
					heat_zone_high_counter++;
				}
			}

			if (heat_zone_high_counter > 5)
			{
				heat_zones.Add(heat_zone_high_value);
				heat_zones_bar.Add(iSwing1.SwingHighBar(0, 7, CurrentBar));
			}
			#endregion

			#region Swing_Low
			#endregion

			Heat_Zones_Check();

			Print_Heat_Zones();
		}

		private void Heat_Zones_Check()
		{
			for (int i = 0; i < heat_zones.Count; i++)
			{
				for (int j = 0; j < heat_zones.Count; j++)
				{
					if (i != j)
					{
						if (heat_zones[i] == heat_zones[j])
						{
							heat_zones.Remove(heat_zones[i]);
						}
					}
				}
			}

			if (heat_zones.Count > 10)
            {
				heat_zones.RemoveAt(0);
				heat_zones_bar.RemoveAt(0);
            }
		}

		private void Print_Heat_Zones()
        {
			for (int i = 0; i < heat_zones.Count; i++)
            {
				Print(string.Format("{0} // {1}", Time[0], heat_zones[i]));
				//Draw.HorizontalLine(this, "Line" + i, heat_zones[i], Brushes.Red);
				//Draw.RegionHighlightY(this, "Range" + i, heat_zones[i] - (iATR[0] * 0.5), heat_zones[i] + (iATR[0] * 0.5), Brushes.Red);
				Draw.Line(this, "Line" + i, iSwing1.SwingHighBar(0, heat_zones_bar[i], CurrentBar), heat_zones[i], 0, heat_zones[i], Brushes.Red);
				Draw.Rectangle(this, "Rectangle" + i, iSwing1.SwingHighBar(0, heat_zones_bar[i], CurrentBar), heat_zones[i] - (iATR[0] * 0.5), 0, heat_zones[i] + (iATR[0] * 0.5), Brushes.Red);
			}
        }
    }
}
