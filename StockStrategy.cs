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
namespace NinjaTrader.NinjaScript.Strategies.Trading
{
	public class MyCustomStrategy : Strategy
	{
		#region Variables
		#region Indicators
		private EMA iEMA1, iEMA2, iEMA3;
		private ATR iATR;
		private VolumeUpDown iVolume;
		private Bollinger iBB;
		private KeltnerChannel iKeltner;
		private VOLMA iVolMA;
		#endregion
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "StockStrategy";
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
				IsUnmanaged = true;
				
				EMA1				= 200;
				EMA2				= 50;
				EMA3				= 20;
				ATR1				= 10;
				BB_Period			= 14;
				BB_Stdv				= 2;
				Keltner_Period		= 10;
				Keltner_Multiplier	= 2;
				VolMA				= 10;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
            {
				// iEMA1 > iEMA2 > iEMA3
				iEMA1			= EMA(Close, EMA1);
				iEMA2			= EMA(Close, EMA2);
				iEMA3			= EMA(Close, EMA3);

				iATR			= ATR(Close, ATR1);

				iVolume			= VolumeUpDown(Close);
				iVolMA			= VOLMA(VolMA);

				iBB				= Bollinger(iEMA3, BB_Stdv, BB_Period);

				iKeltner		= KeltnerChannel(iEMA3, Keltner_Multiplier, Keltner_Period);

				iEMA1.Plots[0].Brush = Brushes.Red;
				iEMA2.Plots[0].Brush = Brushes.Gold;
				iEMA3.Plots[0].Brush = Brushes.Lime;
				iATR.Plots[0].Brush = Brushes.White;
				iVolMA.Plots[0].Brush = Brushes.Yellow;
				iBB.Plots[0].Brush = Brushes.Blue;
				iBB.Plots[1].Brush = Brushes.Blue;
				iBB.Plots[2].Brush = Brushes.Blue;
				iKeltner.Plots[0].Brush = Brushes.Violet;
				iKeltner.Plots[1].Brush = Brushes.Violet;
				iKeltner.Plots[2].Brush = Brushes.Violet;

				AddChartIndicator(iEMA1);
				AddChartIndicator(iEMA2);
				AddChartIndicator(iEMA3);
				AddChartIndicator(iATR);
				AddChartIndicator(iVolume);
				AddChartIndicator(iVolMA);
				AddChartIndicator(iBB);
				AddChartIndicator(iKeltner);
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom strategy logic here.
		}

		#region Parameters
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA1 (Max)", Order = 1, GroupName = "Indicators")]
		public int EMA1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA2 (Mid)", Order = 2, GroupName = "Indicators")]
		public int EMA2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA3 (Min)", Order = 3, GroupName = "Indicators")]
		public int EMA3
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ATR1", Order = 4, GroupName = "Indicators")]
		public int ATR1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Bollinger Bands (Period)", Order = 5, GroupName = "Indicators")]
		public int BB_Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Bollinger Bands (Stdv)", Order = 6, GroupName = "Indicators")]
		public int BB_Stdv
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Keltner Channel (Period)", Order = 7, GroupName = "Indicators")]
		public int Keltner_Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Keltner Channel (Multiplier)", Order = 8, GroupName = "Indicators")]
		public int Keltner_Multiplier
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "VolMA", Order = 9, GroupName = "Indicators")]
		public int VolMA
		{ get; set; }
		#endregion
	}
}
