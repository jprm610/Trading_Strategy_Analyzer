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
		private ATR iATR;
		private VolumeUpDown ivolume_up_down;
		private Range iRange;
		private int Look_back_candles;
		private double Percentile;
		private List<double> volumes = new List<double>();
		private List<double> ranges = new List<double>();
		#endregion
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "MomentumTrigger";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				iATR = ATR(Close, 1);

				ivolume_up_down = VolumeUpDown();

				iRange = Range();

				AddChartIndicator(iATR);
				AddChartIndicator(ivolume_up_down);
				AddChartIndicator(iRange);
			}
		}

		protected override void OnBarUpdate()
		{
			#region Parameters
			Look_back_candles = 10;
			Percentile = 0.95;
            #endregion

            #region Chart_Initialization
            if (BarsInProgress != 0)
				return;

			if (CurrentBars[0] < 0)
				return;

			if (CurrentBar < Look_back_candles)
				return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iATR[0] == 0 || iRange[0] == 0 || Volume[0] == 0)
				return;
			#endregion

			for (int i = 0; i < Look_back_candles; i++)
			{
				volumes.Add(Volume[i]);
				ranges.Add(iRange[i]);
			}
			Lists_Cleaning(volumes);
			Lists_Cleaning(ranges);

			Print(Time[0]);
			for (int i = 0; i < Look_back_candles; i++)
            {
				Print(string.Format("{0} // {1}", volumes[i], ranges[i]));
            }
			Print("------------------------------------------------------");
		}

		private void Lists_Cleaning(List<double> list)
        {
			while (list.Count > Look_back_candles)
            {
				list.RemoveAt(0);
            }
        }
	}
}
