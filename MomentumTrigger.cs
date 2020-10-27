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
using System.Windows.Controls;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class MomentumTrigger : Strategy
	{
		#region Variables
		private Range iRange;
		private double ranges_percentile;
		private List<MyRanges> ranges = new List<MyRanges>();
		private List<MyRanges> ranges_over_percentile = new List<MyRanges>();
		#endregion
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Enter the description for your new custom Strategy here.";
				Name = "MomentumTrigger";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
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

				Look_back_candles = 100;
				Percentile_v = 0.99;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				iRange = Range();

				AddChartIndicator(iRange);
			}
		}

		protected override void OnBarUpdate()
		{
			#region Chart_Initialization
			if (BarsInProgress != 0)
				return;

			if (CurrentBars[0] < 0)
				return;

			if (CurrentBar < Look_back_candles)
				return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iRange[0] == 0 || Volume[0] == 0)
				return;
			#endregion

            #region Save_Volume_and_Range
            for (int i = 0; i < Look_back_candles; i++)
			{
				if (Close[i] >= Open[i])
                {
					ranges.Add(new MyRanges()
					{
						value = iRange[i],
						high = High[i],
						low = Low[i],
						bars_ago = i,
						is_up = true
					});
				}
                else
                {
					ranges.Add(new MyRanges()
					{
						value = iRange[i],
						high = High[i],
						low = Low[i],
						bars_ago = i,
						is_up = false
					});
				}
			}
            #endregion

            #region Copy_Volumes_and_Ranges_Values
			double[] ranges_values = new double[ranges.Count];
			for (int i = 0; i < ranges.Count; i++)
			{
				ranges_values[i] = ranges[i].value;
			}
            #endregion

            #region Calculate_and_Evaluate_Percentiles
			ranges_percentile = Percentile(ranges_values, Percentile_v);

			for (int i = 0; i < Look_back_candles; i++)
			{
				if (ranges[i].value >= ranges_percentile)
                {
					ranges_over_percentile.Add(new MyRanges()
					{
						value = ranges[i].value,
						high = ranges[i].high,
						low = ranges[i].low,
						bars_ago = ranges[i].bars_ago,
						is_up = ranges[i].is_up
					});
				}
            }
            #endregion

            #region Print_Values_over_Percentile
			for (int i = 0; i < ranges_over_percentile.Count; i++)
            {
				if (ranges_over_percentile[i].is_up)
                {
					Draw.TriangleUp(this, "Range_Up" + (CurrentBar - ranges_over_percentile[i].bars_ago), true, ranges_over_percentile[i].bars_ago, ranges_over_percentile[i].low - (TickSize * 30), Brushes.White);
				}
                else
                {
					Draw.TriangleDown(this, "Range_Down" + (CurrentBar - ranges_over_percentile[i].bars_ago), true, ranges_over_percentile[i].bars_ago, ranges_over_percentile[i].high + (TickSize * 30), Brushes.White);
				}				
            }
			#endregion

            #region Reset_Arrays
			ranges.Clear();

			ranges_over_percentile.Clear();
            #endregion
        }

		#region Parameters
		[NinjaScriptProperty]
		[Display(Name = "Look_back_candles", Order = 1, GroupName = "Parameters")]
		public int Look_back_candles
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Percentile", Order = 2, GroupName = "Parameters")]
		public double Percentile_v
		{ get; set; }
		#endregion

		public class MyRanges
        {
			public double value;
			public double high;
			public double low;
			public int bars_ago;
			public bool is_up;
        }

		public double Percentile(double[] sequence, double excelPercentile)
		{
			Array.Sort(sequence);
			int N = sequence.Length;
			double n = (N - 1) * excelPercentile + 1;
			// Another method: double n = (N + 1) * excelPercentile;
			if (n == 1d) return sequence[0];
			else if (n == N) return sequence[N - 1];
			else
			{
				int k = (int)n;
				double d = n - k;
				return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
			}
		}
	}
}
