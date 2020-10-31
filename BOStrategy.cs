#region Using declarations
using System;
using System.Collections;
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
using System.Reflection;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
	public class BOStrategy : Strategy
	{
		#region Variables
		private SMA iSMA1, iSMA2, iSMA3;
		private ATR iATR;
		private Swing iSwing1, iSwing2, iSwing3;
		private int cross_above_bar, cross_below_bar, amount_long, fix_amount_long, amount_short, fix_amount_short, max_indicator_bar_calculation;
		private double ATR_crossing_value, SMA_dis, stop_price_long, trigger_price_long, stop_price_short, trigger_price_short, swingHigh2_max, swingLow2_min;
		private double last_max_high_swingHigh1, last_max_high_swingHigh2, last_min_low_swingLow1, last_min_low_swingLow2, distance_to_BO;
		private double swingHigh2_max_reentry, swingLow2_min_reentry, max_high_swingHigh1, max_high_swingHigh2, min_low_swingLow1, min_low_swingLow2;
		private double last_swingHigh3, last_swingLow3, last_swingHigh2, last_swingLow2, last_swingHigh1, last_swingLow1, current_ATR, current_stop, fix_stop_price_long, fix_trigger_price_long, fix_stop_price_short, fix_trigger_price_short;
		private bool is_incipient_up_trend, is_incipient_down_trend, is_upward, is_downward, gray_ellipse_long, gray_ellipse_short, is_reentry_long, is_reentry_short, is_long, is_short, isBO, is_BO_up_swing1, is_BO_up_swing2, is_BO_down_swing1, is_BO_down_swing2;

		#region Momentum_Process_Variables
		private Range iRange;
		private List<MyRanges> ranges_over_percentile = new List<MyRanges>();
		#endregion

		#region Heat_Zones_Process_Variables
		private List<MySwing> swings2_high = new List<MySwing>();
		private List<MySwing> swings2_low = new List<MySwing>();
		private List<MySwing> swings3_high = new List<MySwing>();
		private List<MySwing> swings3_low = new List<MySwing>();
		private List<MyHeatZone> heat_zones = new List<MyHeatZone>();
		#endregion

		private bool cross_below_iSMA3_to_iSMA2, cross_above_iSMA3_to_iSMA2;
		private Account myAccount;
		private Order my_entry_order = null, my_entry_market = null, my_exit_order = null;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Strategy that Operates Breakouts";
				Name = "BOStrategy";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = false;
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

				SMA1 = 200;
				SMA2 = 50;
				SMA3 = 20;
				ATR1 = 100;
				Swing1 = 4;
				Swing2 = 14;
				Swing3 = 36;
				UnitsTriggerForTrailing = 1;
				TrailingUnitsStop = 5;
				RiskUnit = 1;
				IncipientTrandFactor = 2;
				ATRStopFactor = 3;
				TicksToBO = 10;
				SwingPenetration = 50;
				ClosnessFactor = 2;
				ClosnessToTrade = 1;
				MagicNumber = 50;
				Look_back_candles = 100;
				Percentile_v = 0.99;
				Days = 20;
				Heat_zone_strength = 2;
				Width = 1.5;
			}
			else if (State == State.Configure)
			{
				AddDataSeries("GBPUSD", Data.BarsPeriodType.Minute, 60, Data.MarketDataType.Last);
			}
			else if (State == State.DataLoaded)
			{
				iRange = Range();

				// iSMA1 > iSMA2 > iSMA3
				iSMA1 = SMA(Close, SMA1);
				iSMA2 = SMA(Close, SMA2);
				iSMA3 = SMA(Close, SMA3);

				iATR = ATR(Close, ATR1);

				// iSwing1 < iSwing2
				iSwing1 = Swing(Close, Swing1);
				iSwing2 = Swing(Close, Swing2);
				iSwing3 = Swing(Close, Swing3);

				iSMA1.Plots[0].Brush = Brushes.Red;
				iSMA2.Plots[0].Brush = Brushes.Gold;
				iSMA3.Plots[0].Brush = Brushes.Lime;
				iATR.Plots[0].Brush = Brushes.White;
				iSwing1.Plots[0].Brush = Brushes.Fuchsia;
				iSwing1.Plots[1].Brush = Brushes.Gold;
				iSwing2.Plots[0].Brush = Brushes.Silver;
				iSwing2.Plots[1].Brush = Brushes.Silver;
				iSwing3.Plots[0].Brush = Brushes.DarkCyan;
				iSwing3.Plots[1].Brush = Brushes.DarkCyan;

				AddChartIndicator(iSMA1);
				AddChartIndicator(iSMA2);
				AddChartIndicator(iSMA3);
				AddChartIndicator(iATR);
				AddChartIndicator(iSwing1);
				AddChartIndicator(iSwing2);
				AddChartIndicator(iSwing3);
				AddChartIndicator(iRange);
			}
		}

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled,
			double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			// Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
			// This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
			if (order.Name == "entryOrder")
			{
				my_entry_order = order;
			}

			if (order.Name == "entryMarket")
			{
				my_entry_market = order;
			}

			if (order.Name == "exit")
			{
				my_exit_order = order;
			}
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId,
			DateTime time)
		{
			// Reset our stop order and target orders' Order objects after our position is closed. (1st Entry)
			if (my_exit_order != null)
			{
				if (my_exit_order == execution.Order)
				{
					if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
					{
						if (my_entry_order != null)
						{
							my_entry_order.OrderState = OrderState.Cancelled;
						}

						if (my_entry_market != null)
						{
							my_entry_market.OrderState = OrderState.Cancelled;
						}
					}
				}
			}

			if (my_entry_market != null)
			{
				if (my_entry_market == execution.Order)
				{
					if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
					{
						if (my_entry_order != null)
						{
							if (my_entry_order.OrderState == OrderState.Filled)
							{
								my_entry_order.OrderState = OrderState.Cancelled;
							}
						}
					}
				}
			}
		}

		protected override void OnBarUpdate()
		{
			#region Chart_Initialization
			if (BarsInProgress != 0)
				return;

			if (CurrentBars[0] < 0)
				return;

			if (CurrentBar < BarsRequiredToTrade || CurrentBar < max_indicator_bar_calculation)
				return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iSwing1.SwingHigh[0] == 0 || iSwing1.SwingLow[0] == 0 ||
				iSwing2.SwingHigh[0] == 0 || iSwing2.SwingLow[0] == 0 ||
				iSwing3.SwingHigh[0] == 0 || iSwing3.SwingLow[0] == 0 ||
				iATR[0] == 0 ||
				iRange[0] == 0)
				return;

			//Review that the swings are charged to avoid later bugs. In other words, to check if the needed swing instances exist.
			for (int i = 0; i <= (Days * 24); i++)
			{
				if (iSwing3.SwingHigh[i] == 0)
					return;

				if (iSwing3.SwingLow[i] == 0)
					return;
			}
			#endregion

			#region Variable_Reset

			//Reset is_BO variable once a new swing comes up.
			#region BO_reset
			if (last_swingHigh1 != iSwing1.SwingHigh[0])
				is_BO_up_swing1 = false;

			if (last_swingHigh2 != iSwing2.SwingHigh[0])
				is_BO_up_swing2 = false;

			if (last_swingLow1 != iSwing1.SwingLow[0])
				is_BO_down_swing1 = false;

			if (last_swingLow2 != iSwing2.SwingLow[0])
				is_BO_down_swing2 = false;
			#endregion

			//If there isn't an active position, the trend flags (is_long, is_short) are reset to false for later calculations.
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				is_long = false;
				is_short = false;
			}

			last_max_high_swingHigh1 = max_high_swingHigh1;
			last_max_high_swingHigh2 = max_high_swingHigh2;
			last_min_low_swingLow1 = min_low_swingLow1;
			last_min_low_swingLow2 = min_low_swingLow2;

			is_reentry_long = false;
			is_reentry_short = false;
			#endregion

			#region Variable_Initialization
			current_ATR = iATR[0];
			current_stop = current_ATR * ATRStopFactor;
			distance_to_BO = TicksToBO * TickSize;

			//Save the distance between the 2 biggest SMAs.
			SMA_dis = Math.Abs(iSMA1[0] - iSMA2[0]);
			#endregion

			#region Parameters_Check
			//This block of code checks if the indicator values that will be used in later calculations are correct.
			//When a SMA of period 200 prints a value in the bar 100 is an example of a wrong indicator value.
			//This conditional only executes once, that is why its argument.
			if (max_indicator_bar_calculation == 0)
			{
				//Create an array so that we can iterate through the values.
				int[] indicators = new int[4];
				indicators[0] = SMA1;
				indicators[1] = SMA2;
				indicators[2] = SMA3;
				indicators[3] = ATR1;

				//Find the max indicator calculation value.
				for (int i = 0; i < 4; i++)
				{
					if (indicators[i] > max_indicator_bar_calculation)
					{
						max_indicator_bar_calculation = indicators[i];
					}
				}

				//Stop calculations if the chart is not printing correct indicator values.
				if (CurrentBar < max_indicator_bar_calculation)
				{
					Print(string.Format("BarsRequiredToTrade has been updated to {0} to avoid wrong indicator values.", max_indicator_bar_calculation));
					return;
				}
			}

			//Make sure that the Trailing_Stop won't be set below/above the maximum/minimum stop point. 
			//This by setting the UnitsTriggerForTrailing by 1 below the TrailingUnitsStop if necessary.
			if (TrailingUnitsStop - UnitsTriggerForTrailing > 1)
			{
				UnitsTriggerForTrailing = TrailingUnitsStop - 1;
				Print(string.Format("UnitsTriggerForTrailing has been set to {0} to avoid stop conflicts.", UnitsTriggerForTrailing));
			}

			//This condition evaluates if the SMAs parameters are set incorrectly or unsorted.
			//The SMAs values have to be set like the following: SMA1 > SMA2 > SMA3.
			//This block of code sorts (with Bubble Sort) and sets sorted values to the SMAs. 
			if (SMA1 < SMA2 || SMA1 < SMA3 || SMA2 < SMA3)
			{
				//Create an array so that we can iterate through the values.
				int[] SMAv = new int[3];
				SMAv[0] = SMA1;
				SMAv[1] = SMA2;
				SMAv[2] = SMA3;

				//Sort using the Bubble Sort algorithm.
				bool isSorted = false;
				while (!isSorted)
				{
					isSorted = true;
					for (int i = 0; i < 2; i++)
					{
						if (SMAv[i] < SMAv[i + 1])
						{
							int tmp = SMAv[i];
							SMAv[i] = SMAv[i + 1];
							SMAv[i + 1] = tmp;
							isSorted = false;
						}
					}
				}

				//Asign sorted values to the SMAs indicators.
				iSMA1 = SMA(Close, SMAv[0]);
				iSMA2 = SMA(Close, SMAv[1]);
				iSMA3 = SMA(Close, SMAv[2]);

				//This reassignment is done to avoid this conditional to be executed again.
				SMA1 = SMAv[0];
				SMA2 = SMAv[1];
				SMA3 = SMAv[2];

				Print(string.Format("The SMAs have been sorted as follow: iSMA1: {0} // iSMA2: {1} // iSMA3: {2}", SMAv[0], SMAv[1], SMAv[2]));
			}

			//This condition evaluates if the SMAs parameters are set incorrectly or unsorted.
			//The Swings values have to be set like the following: Swing1 < Swing2.
			//This block of code sorts and sets sorted values to the Swings with a simple swap.
			if (Swing2 < Swing1)
			{
				//Swap
				int tmp = Swing2;
				Swing2 = Swing1;
				Swing1 = tmp;

				//Asign sorted values to the Swing indicators.
				iSwing1 = Swing(Close, Swing1);
				iSwing2 = Swing(Close, Swing2);

				Print(string.Format("The Swings have been sorted as follow: iSwing1: {0} // iSwing2: {1}.", Swing1, Swing2));
			}
			#endregion

			#region Momentum_Process
			{
				List<MyRanges> ranges = new List<MyRanges>();

				#region Save_Ranges
				{
					//Save the last n ranges (n = Look_back_candles) using its class.
					//Saving its value, the bar when it happened, is high and low, 
					//and wheter it happened in a green or red candle (is_up).
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
				}
				#endregion

				#region Copy_Ranges_Values
					//Traspass the ranges values to an array of double (ranges_values)
					//in order to input the values in the Percentile function 
					//which will appear in the next region.
					double[] ranges_values = new double[ranges.Count];
					for (int i = 0; i < ranges.Count; i++)
					{
						ranges_values[i] = ranges[i].value;
					}
				#endregion

				#region Calculate_and_Evaluate_Percentiles
				{
					//Find the value equivalent to the Percentile parameter.
					double ranges_percentile = Percentile(ranges_values, Percentile_v);

					//Filter the ranges that are over the Percentile 
					//and save them in another list which is going to be printed in the next region.  
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
				}
				#endregion

				#region Print_Values_over_Percentile
				{
					//Print triangles in the candles where the range surpassed the Percentile value.
					//If the candle is green, print the triangle below the candle pointing upwards.
					//If the candle is red, print the triangle above the candle pointing downwards.
					//Both meaning the possible momentum direction of the market.
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
				}
				#endregion

				#region Reset_Arrays
				{
					ranges.Clear();
					ranges_over_percentile.Clear();
				}
				#endregion
			}
			#endregion

			#region Heat_Zones_Process
			{
				//Define the look back period for the swing calculation converting the Days parameters into candles.
				//Define the width of the possible heat zone, using the Width parameter.
				int candles_look_back = Days * 24;
				double width = Width * iATR[0];

				#region Save_Swings
				{
					#region Swings3
					//In each swing3 update, save the last n swings highs and lows, with its corresponding CurrentBar. (n = Instance)
					//This swings are saved in a list (swings3_high and swings3_low) of class MySwing which is defined in the classes region.
					if (last_swingHigh3 != iSwing3.SwingHigh[0] || last_swingLow3 != iSwing3.SwingLow[0])
					{
						//Always clean the lists when a swing comes up.
						swings3_high.Clear();
						swings3_low.Clear();

						//Save all the swing3Highs that appeared in the candles_look_back_period.
						double last_evaluated_swingHigh = 0;
						for (int i = 0; i <= candles_look_back; i++)
						{
							if (iSwing3.SwingHigh[i] != last_evaluated_swingHigh)
							{
								swings3_high.Add(new MySwing()
								{
									value = iSwing3.SwingHigh[i],
									bar = 0,
									is_broken = false,
								});

								last_evaluated_swingHigh = iSwing3.SwingHigh[i];
							}
						}						
						//Then save its CurrentBar that is going to be our ID.
						for (int i = 1; i <= swings3_high.Count; i++)
						{
							swings3_high[i - 1].bar = CurrentBar - iSwing3.SwingHighBar(0, i, CurrentBar);
						}

						//Save all the swing3Lows that appeared in the candles_look_back_period.
						double last_evaluated_swingLow = 0;
						for (int i = 0; i <= candles_look_back; i++)
						{
							if (iSwing3.SwingLow[i] != last_evaluated_swingLow)
							{
								swings3_low.Add(new MySwing()
								{
									value = iSwing3.SwingLow[i],
									bar = 0,
									is_broken = false,
								});

								last_evaluated_swingLow = iSwing3.SwingLow[i];
							}
						}
						//Then save its CurrentBar that is going to be our ID.
						for (int i = 1; i <= swings3_low.Count; i++)
						{
							swings3_low[i - 1].bar = CurrentBar - iSwing3.SwingLowBar(0, i, CurrentBar);
						}
					}
					#endregion

					#region Swings2
					//In each swing2 update, save the last n swings highs and lows, with its corresponding CurrentBar. (n = Instance)
					//This swings are saved in a list (swings2_high and swings2_low) of class MySwing which is defined in the classes region.
					if (last_swingHigh2 != iSwing2.SwingHigh[0] || last_swingLow2 != iSwing2.SwingLow[0])
					{
						//Extend the candles look back if needed.
						if (swings3_high[swings3_high.Count - 1].bar <= swings3_low[swings3_low.Count - 1].bar)
						{
							candles_look_back = CurrentBar - swings3_high[swings3_high.Count - 1].bar;
						}
						else
						{
							candles_look_back = CurrentBar - swings3_low[swings3_low.Count - 1].bar;
						}

						//Always clean the lists when a swing comes up.
						swings2_high.Clear();
						swings2_low.Clear();

						//Save all the swing2Highs that appeared in the candles_look_back_period.
						double last_evaluated_swingHigh = 0;
						for (int i = 0; i <= candles_look_back; i++)
						{
							if (iSwing2.SwingHigh[i] != last_evaluated_swingHigh)
							{
								swings2_high.Add(new MySwing()
								{
									value = iSwing2.SwingHigh[i],
									bar = 0,
									is_broken = false,
								});

								last_evaluated_swingHigh = iSwing2.SwingHigh[i];
							}
						}
						//Then save its CurrentBar that is going to be our ID.
						for (int i = 1; i <= swings2_high.Count; i++)
						{
							swings2_high[i - 1].bar = CurrentBar - iSwing2.SwingHighBar(0, i, CurrentBar);
						}

						//Save all the swing2Lows that appeared in the candles_look_back_period.
						double last_evaluated_swingLow = 0;
						for (int i = 0; i <= candles_look_back; i++)
						{
							if (iSwing2.SwingLow[i] != last_evaluated_swingLow)
							{
								swings2_low.Add(new MySwing()
								{
									value = iSwing2.SwingLow[i],
									bar = 0,
									is_broken = false,
								});

								last_evaluated_swingLow = iSwing2.SwingLow[i];
							}
						}
						//Then save its CurrentBar that is going to be our ID.
						for (int i = 1; i <= swings2_low.Count; i++)
						{
							swings2_low[i - 1].bar = CurrentBar - iSwing2.SwingLowBar(0, i, CurrentBar);
						}
					}
                    #endregion                    
				}
				#endregion

				#region Remove_Repeated_Swings
				{
					//Knowing that a Swing3 is also a Swing2, the following loops
					//clean the swings2 that asre swings3 using its ID (CurrentBar) for its later use.

					for (int i = 0; i < swings3_high.Count; i++)
					{
						for (int j = 0; j < swings2_high.Count; j++)
						{
							if (swings3_high[i].bar == swings2_high[j].bar)
							{
								swings2_high.RemoveAt(j);
							}
						}
					}

					for (int i = 0; i < swings3_low.Count; i++)
					{
						for (int j = 0; j < swings2_low.Count; j++)
						{
							if (swings3_low[i].bar == swings2_low[j].bar)
							{
								swings2_low.RemoveAt(j);
							}
						}
					}
				}
				#endregion

				#region is_broken Evaluation
				{
					//Determine which swings (3 or 4) are broken,
					//this in order to determine if it is a Heat Zone or not.

					for (int i = 0; i < swings3_high.Count; i++)
					{
						for (int j = 0; j < CurrentBar - swings3_high[i].bar; j++)
						{
							if (High[j] > swings3_high[i].value)
							{
								swings3_high[i].is_broken = true;
							}
						}
					}

					for (int i = 0; i < swings3_low.Count; i++)
					{
						for (int j = 0; j < CurrentBar - swings3_low[i].bar; j++)
						{
							if (Low[j] < swings3_low[i].value)
							{
								swings3_low[i].is_broken = true;
							}
						}
					}

					for (int i = 0; i < swings2_high.Count; i++)
					{
						for (int j = 0; j < CurrentBar - swings2_high[i].bar; j++)
						{
							if (High[j] > swings2_high[i].value)
							{
								swings2_high[i].is_broken = true;
							}
						}
					}

					for (int i = 0; i < swings2_low.Count; i++)
					{
						for (int j = 0; j < CurrentBar - swings2_low[i].bar; j++)
						{
							if (Low[j] < swings2_low[i].value)
							{
								swings2_low[i].is_broken = true;
							}
						}
					}
				}
				#endregion

				#region Heat_Zone 
				{
					//Clean the Heat_zones list due to its constant change in time.
					heat_zones.Clear();

					#region Swings3_Based
					{
						//Every swing3 that is not broken will be added as a Heat_Zone

						for (int i = 0; i < swings3_high.Count; i++)
						{
							if (!swings3_high[i].is_broken)
							{
								heat_zones.Add(new MyHeatZone()
								{
									value = swings3_high[i].value
								});
							}
						}

						for (int i = 0; i < swings3_low.Count; i++)
						{
							if (!swings3_low[i].is_broken)
							{
								heat_zones.Add(new MyHeatZone()
								{
									value = swings3_low[i].value
								});
							}
						}
					}
					#endregion

					#region Swings2_Based
					{
						//If a swing2High happened later than a swing3 (Low or high) that is broken,
						//this swing can confirm that there is a Heat Zone at that price level and 
						//that is why it is going to be added as a Heat Zone.
						for (int i = 0; i < swings2_high.Count; i++)
						{
							for (int j = 0; j < swings3_high.Count; j++)
							{
								if (swings3_high[j].is_broken && !swings2_high[i].is_broken &&
									swings3_high[j].bar < swings2_high[i].bar)
								{
									double swing_distance = Math.Abs(swings2_high[i].value - swings3_high[j].value);

									if (swing_distance <= width)
									{
										heat_zones.Add(new MyHeatZone()
										{
											value = swings2_high[i].value
										});
									}
								}
							}

							for (int j = 0; j < swings3_low.Count; j++)
							{
								if (swings3_low[j].is_broken && !swings2_high[i].is_broken &&
									swings3_low[j].bar < swings2_high[i].bar)
								{
									double swing_distance = Math.Abs(swings2_high[i].value - swings3_low[j].value);

									if (swing_distance <= width)
									{
										heat_zones.Add(new MyHeatZone()
										{
											value = swings2_high[i].value
										});
									}
								}
							}
						}

						//If a swing2Low happened later than a swing3 (Low or high) that is broken,
						//this swing can confirm that there is a Heat Zone at that price level and 
						//that is why it is going to be added as a Heat Zone.
						for (int i = 0; i < swings2_low.Count; i++)
						{
							for (int j = 0; j < swings3_high.Count; j++)
							{
								if (swings3_high[j].is_broken && !swings2_low[i].is_broken &&
									swings3_high[j].bar < swings2_low[i].bar)
								{
									double swing_distance = Math.Abs(swings2_low[i].value - swings3_high[j].value);

									if (swing_distance <= width)
									{
										heat_zones.Add(new MyHeatZone()
										{
											value = swings2_low[i].value
										});
									}
								}
							}

							for (int j = 0; j < swings3_low.Count; j++)
							{
								if (swings3_low[j].is_broken && !swings2_low[i].is_broken &&
									swings3_low[j].bar < swings2_low[i].bar)
								{
									double swing_distance = Math.Abs(swings2_low[i].value - swings3_low[j].value);

									if (swing_distance <= width)
									{
										heat_zones.Add(new MyHeatZone()
										{
											value = swings2_low[i].value
										});
									}
								}
							}
						}
					}
					#endregion

					#region Heat_Zones_Cleaning
					{
						//First remove the Heat Zones that have the same price level (Basically that are repeated).
						for (int i = 0; i < heat_zones.Count; i++)
						{
							for (int j = 0; j < heat_zones.Count; j++)
							{
								if (i != j)
								{
									if (heat_zones[i] == heat_zones[j])
									{
										heat_zones.RemoveAt(j);
									}
								}
							}
						}

						//If there are 2 Heat Zones that are close enough (Less or equal to the width),
						//determine which is closer to the current price and erase the further one. 
						for (int i = 0; i < heat_zones.Count; i++)
						{
							for (int j = 0; j < heat_zones.Count; j++)
							{
								if (i != j)
								{
									double heat_zones_distance = Math.Abs(heat_zones[i].value - heat_zones[j].value);

									if (heat_zones_distance <= width)
									{
										double price_distance_i = Math.Abs(Open[0] - heat_zones[i].value);
										double price_distance_j = Math.Abs(Open[0] - heat_zones[j].value);

										if (price_distance_i <= price_distance_j)
										{
											heat_zones[j].is_removable = true;
										}
										else
										{
											heat_zones[i].is_removable = true;
										}
									}
								}
							}
						}

						for (int i = 0; i < heat_zones.Count; i++)
						{
							if (heat_zones[i].is_removable)
							{
								heat_zones.RemoveAt(i);
							}
						}

						//Re-check if there are repeated Heat Zones.
						for (int i = 0; i < heat_zones.Count; i++)
						{
							for (int j = 0; j < heat_zones.Count; j++)
							{
								if (i != j)
								{
									if (heat_zones[i] == heat_zones[j])
									{
										heat_zones.RemoveAt(j);
									}
								}
							}
						}
					}
                    #endregion
                }
				#endregion

				#region Swings_Update_Reset
				{
					//Reset the last swing value in order to execute the swing update processes.
					last_swingHigh1 = iSwing1.SwingHigh[0];
					last_swingLow1 = iSwing1.SwingLow[0];
					last_swingHigh2 = iSwing2.SwingHigh[0];
					last_swingLow2 = iSwing2.SwingLow[0];
					last_swingHigh3 = iSwing3.SwingHigh[0];
					last_swingLow3 = iSwing3.SwingLow[0];
				}
				#endregion
            }
            #endregion

            #region Overall_Market_Movement
            //If the second biggest SMA crosses above the biggest SMA it means the market is going upwards.
            //The ATR value is saved immediately, the is_upward flag is set to true while its opposite flag (is_downward) is set to false.
            //Finally the CurrentBar of the event is saved for later calculations.
            if (CrossAbove(iSMA2, iSMA1, 1))
			{
				ATR_crossing_value = iATR[0];
				is_upward = true;
				is_downward = false;
				cross_above_bar = CurrentBar;
			}

			//If the second biggest SMA crosses below the biggest SMA it means the market is going downwards.
			//The ATR value is saved immediately, the is_downward flag is set to true while its opposite flag (is_upward) is set to false.
			//Finally the CurrentBar of the event is saved for later calculations.
			else if (CrossBelow(iSMA2, iSMA1, 1))
			{
				ATR_crossing_value = iATR[0];
				is_downward = true;
				is_upward = false;
				cross_below_bar = CurrentBar;
			}
			#endregion

			#region Incipient_Trend_Identification
			//IncipientTrend: There has been a crossing event with the 2 biggest SMAs and
			//the distance between them is greater or equal to the ATR value at the SMAs crossing event multiplied by the IncipientTrendFactor parameter.

			//If the overall market movement is going upwards and the Incipient Trend is confirmed, the is_incipient_up_trend flag is set to true
			//while its opposite (is_incipient_down_trend) is set to false and the gray_ellipse_short flag is set to false.
			if (is_upward &&
				(SMA_dis >= IncipientTrandFactor * ATR_crossing_value))
			{
				is_incipient_up_trend = true;
				is_incipient_down_trend = false;
				gray_ellipse_short = false;
			}

			//If the overall market movement is going downwards and the Incipient Trend is confirmed, the is_incipient_down_trend flag is set to true
			//while its opposite (is_incipient_up_trend) is set to false and the gray_ellipse_long flag is set to false.		
			else if (is_downward &&
				(SMA_dis >= IncipientTrandFactor * ATR_crossing_value))
			{
				is_incipient_down_trend = true;
				is_incipient_up_trend = false;
				gray_ellipse_long = false;
			}
			#endregion

			#region Gray_Ellipse
			//Gray Ellipse: Is when the smallest and second biggest SMAs crosses in the opposite direction of the overall market movement.

			#region Long
			//This if statement recognizes the cross below event between the smallest and second biggest SMAs;
			//nevertheless, it does not mean that a gray ellipse event has happened.
			if (CrossBelow(iSMA3, iSMA2, 1))
			{
				cross_below_iSMA3_to_iSMA2 = true;
			}

			//If both events (Incipient Trend event and smallest SMAs opposite cross) happens it means that a Gray Ellipse event has happened.
			//That is why the gray_ellipse_long flag is set to true while the cross_below_iSMA3_to_iSMA2 is reset to false for next events.
			//Then there is going to be an iteration in order to find the highest swingHigh14 between the biggest SMA cross event and the gray ellipse.
			if (is_incipient_up_trend && cross_below_iSMA3_to_iSMA2)
			{
				cross_below_iSMA3_to_iSMA2 = false;
				gray_ellipse_long = true;

				int SMAs_cross_and_gray_ellipse_dis = CurrentBar - cross_above_bar;
				//This if statement is done to avoid the bug when trying to find the value of swing indicator beyond the 256 MaximunBarLookBack period, 
				//which is not possible.
				if (SMAs_cross_and_gray_ellipse_dis > 240)
					SMAs_cross_and_gray_ellipse_dis = 240;

				//Iterate to find the highest swingHigh2
				swingHigh2_max = iSwing2.SwingHigh[0];
				for (int i = 0; i <= SMAs_cross_and_gray_ellipse_dis; i++)
				{
					if (iSwing2.SwingHigh[i] > swingHigh2_max)
					{
						swingHigh2_max = iSwing2.SwingHigh[i];
					}
				}
			}
			//If the swingHigh of the current bar surpass the Max Level of the swingHigh (bigggest swing) where the gray_ellipse_long was originated,
			//and the current swing is right next to the gray_ellipse swing it means that a Reentry trade can be executed.
			//That is why the is_reentry_long flag is set to true and the current swingHigh (biggest swing) is saved to keep track of the last swing where a Reentry
			//trade can be executed.
			else if (gray_ellipse_long &&
				(iSwing2.SwingHigh[0] > swingHigh2_max))
			{
				is_reentry_long = true;
				swingHigh2_max_reentry = iSwing2.SwingHigh[0];
			}

			//If the current high surpass the swingHigh where a Reentry trade can be executed, it means that this trade type can't be done again
			//before another gray_ellipse.
			//That is why the gray_ellipse_long flag and the is_reentry_long flag are set to false.
			if (is_reentry_long &&
				(High[0] > swingHigh2_max_reentry))
			{
				gray_ellipse_long = false;
				is_reentry_long = false;
			}
			#endregion

			#region Short
			//This if statement recognizes the cross above event between the smallest and second biggest SMAs;
			//nevertheless, it does not mean that a gray ellipse event has happened.
			if (CrossAbove(iSMA3, iSMA2, 1))
			{
				cross_above_iSMA3_to_iSMA2 = true;
			}

			//If both events (Incipient Trend event and smallest SMAs opposite cross) happens it means that a Gray Ellipse event has happened.
			//That is why the gray_ellipse_short flag is set to true while the cross_below_iSMA3_to_iSMA2 is reset to false for next events.
			//Then there is going to be an iteration in order to find the lowest swingLow14 between the biggest SMA cross event and the gray ellipse.
			if (is_incipient_down_trend && cross_above_iSMA3_to_iSMA2)
			{
				cross_above_iSMA3_to_iSMA2 = false;
				gray_ellipse_short = true;

				int SMAs_cross_and_gray_ellipse_dis = CurrentBar - cross_below_bar;
				//This if statement is done to avoid the bug when trying to find the value of swing indicator beyond the 256 MaximunBarLookBack period, 
				//which is not possible.
				if (SMAs_cross_and_gray_ellipse_dis > 240)
					SMAs_cross_and_gray_ellipse_dis = 240;

				//Iterate to find the lowest swingLow14
				swingLow2_min = iSwing2.SwingLow[0];
				for (int i = 0; i <= SMAs_cross_and_gray_ellipse_dis; i++)
				{
					if (iSwing2.SwingLow[i] < swingLow2_min)
					{
						swingLow2_min = iSwing2.SwingLow[i];
					}
				}
			}
			//If the swingLow of the current bar surpass the Min Level of the swingLow (bigggest swing) where the gray_ellipse_short was originated,
			//and the current swing is right next to the gray_ellipse swing it means that a Reentry trade can be executed.
			//That is why the is_reentry_short flag is set to true and the current swingLow (biggest swing) is saved to keep track of the last swing where a Reentry
			//trade can be executed.
			else if (gray_ellipse_short &&
				(iSwing2.SwingLow[0] < swingLow2_min))
			{
				is_reentry_short = true;
				swingLow2_min_reentry = iSwing2.SwingLow[0];
			}

			//If the current low surpass the swingLow where a Reentry trade can be executed, it means that this trade type can't be done again
			//before another gray_ellipse.
			//That is why the gray_ellipse_short flag and the is_reentry_short flag are set to false.
			if (is_reentry_short &&
				(Low[0] < swingLow2_min_reentry))
			{
				gray_ellipse_short = false;
				is_reentry_short = false;
			}
			#endregion
			#endregion

			#region Trade_Identification
			#region Red_Trade
			//Red Trades are executed every time the price crosses (above/below) the SMA1.
			#region Long
			//If the overall market movement is downwards and the current position is short or there is not a current position yet,
			//evaluate if there is a swing near the SMA1 that represents a posible crossing movement.
			if (is_incipient_down_trend &&
				(Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short))
			{
				bool is_swingHigh1 = false;
				bool is_swingHigh2 = false;
				bool is_active_long_position = false;

				//Validate whether there is a valid swingHigh (swing1) near the SMA1, that is to say that the swing is located in a distance less or 
				//equal than the current ATR multiplied by the ClosnessToTrade parameter from the SMA1.
				if (iSwing1.SwingHigh[0] >= iSMA1[0] - (iATR[0] * ClosnessToTrade))
				{
					//If the Stop range contains at least one of the 2 biggest SMAs
					//call the Swing1 function and store the flags in the three variables below for its later use.
					if ((current_stop >= iSwing1.SwingHigh[0] + distance_to_BO - iSMA1[0]) ||
						(current_stop >= iSwing1.SwingHigh[0] + distance_to_BO - iSMA2[0]))
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA1, true);
						is_swingHigh1 = ReturnedValues.Item1;
						is_active_long_position = ReturnedValues.Item2;
						is_BO_up_swing1 = ReturnedValues.Item3;
					}
				}

				//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
				//don't evaluate this conditional statement.
				if (!is_swingHigh1 && !is_active_long_position)
				{
					//Validate whether there is a valid swingHigh (swing2) near the SMA1, that is to say that the swing is located in a distance less or equal than the 
					//current ATR multiplied by the ClosnessToTrade parameter from the SMA1.
					if (iSwing2.SwingHigh[0] >= iSMA1[0] - (iATR[0] * ClosnessToTrade))
					{
						//If the Stop range contains at least one of the 2 biggest SMAs
						//call the Swing2 function and store the flags in the three variables below for its later use.
						if ((current_stop >= iSwing2.SwingHigh[0] + distance_to_BO - iSMA1[0]) ||
							(current_stop >= iSwing2.SwingHigh[0] + distance_to_BO - iSMA2[0]))
						{
							Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA1, true);
							is_swingHigh2 = ReturnedValues.Item1;
							is_active_long_position = ReturnedValues.Item2;
							is_BO_up_swing2 = ReturnedValues.Item3;
						}
					}
				}
			}
			#endregion

			#region Short
			//If the overall market movement is upwards and the current position is long or there is not a current position yet,
			//evaluate if there is a swing near the SMA1 that represents a posible crossing movement.
			else if (is_incipient_up_trend &&
				(Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long))
			{
				bool is_swingLow1 = false;
				bool is_swingLow2 = false;
				bool is_active_short_position = false;

				//Validate whether there is a valid swingLow (swing1) near the SMA1, that is to say that the swing is located in a distance less or equal than the 
				//current ATR multiplied by the ClosnessToTrade parameter from the SMA1.		
				if (iSwing1.SwingLow[0] <= iSMA1[0] + (iATR[0] * ClosnessToTrade))
				{
					//If the Stop range contains at least one of the 2 biggest SMAs
					//call the Swing1 function and store the flags in the three variables below for its later use.
					if ((current_stop >= iSMA1[0] - iSwing1.SwingLow[0] - distance_to_BO) ||
						(current_stop >= iSMA2[0] - iSwing1.SwingLow[0] - distance_to_BO))
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA1, false);
						is_swingLow1 = ReturnedValues.Item1;
						is_active_short_position = ReturnedValues.Item2;
						is_BO_down_swing1 = ReturnedValues.Item3;
					}
				}

				//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
				//don't evaluate this conditional statement.
				if (!is_swingLow1 && !is_active_short_position)
				{
					//Validate whether there is a valid swingLow (swing2) near the SMA1, that is to say that the swing is located in a distance less or equal than the
					//current ATR multiplied by the ClosnessToTrade parameter from the SMA1.
					if (iSwing2.SwingLow[0] <= iSMA1[0] + (iATR[0] * ClosnessToTrade))
					{
						//If the Stop range contains at least one of the 2 biggest SMAs
						//call the Swing2 function and store the flags in the three variables below for its later use.
						if ((current_stop >= iSMA1[0] - iSwing2.SwingLow[0] - distance_to_BO) ||
							(current_stop >= iSMA2[0] - iSwing2.SwingLow[0] - distance_to_BO))
						{
							Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA1, false);
							is_swingLow2 = ReturnedValues.Item1;
							is_active_short_position = ReturnedValues.Item2;
							is_BO_down_swing2 = ReturnedValues.Item3;
						}
					}
				}
			}
			#endregion
			#endregion

			#region Traditional_Red

			#region Long
			//If the overall market movement is going upwards, the 2 biggest SMAs are separated enough and there is a short position active or the is not an
			//active position yet.
			if (is_incipient_up_trend &&
				(Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short))
			{
				bool is_swingHigh1 = false;
				bool is_swingHigh2 = false;
				bool is_active_long_position = false;

				//Validate whether there is a valid swingHigh1 near the SMA1 and evaluate if the swingHigh1 is above the SMA2.
				if ((iSwing1.SwingHigh[0] >= iSMA1[0] - (iATR[0] * ClosnessToTrade)) &&
					(iSMA2[0] - iSwing1.SwingHigh[0] > iATR[0] * ClosnessFactor))
				{
					//If the Stop range contains the SMA1
					//call the Swing1 function and store the flags in the three variables below for its later use.
					if (current_stop >= iSwing1.SwingHigh[0] + distance_to_BO - iSMA1[0])
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA1, true);
						is_swingHigh1 = ReturnedValues.Item1;
						is_active_long_position = ReturnedValues.Item2;
						is_BO_up_swing1 = ReturnedValues.Item3;
					}
				}

				//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
				//don't evaluate this conditional statement.
				if (!is_swingHigh1 && !is_active_long_position)
				{
					//Validate whether there is a valid swingHigh2 near the SMA1 and evaluate if the swingHigh2 is above the SMA2.
					if ((iSwing2.SwingHigh[0] >= iSMA1[0] - (iATR[0] * ClosnessToTrade)) &&
						(iSMA2[0] - iSwing2.SwingHigh[0] > iATR[0] * ClosnessFactor))
					{
						//If the Stop range contains the SMA1
						//call the Swing2 function and store the flags in the three variables below for its later use.
						if (current_stop >= iSwing2.SwingHigh[0] + distance_to_BO - iSMA1[0])
						{
							Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA1, true);
							is_swingHigh2 = ReturnedValues.Item1;
							is_active_long_position = ReturnedValues.Item2;
							is_BO_up_swing2 = ReturnedValues.Item3;
						}
					}
				}
			}
			#endregion

			#region Short
			//If the overall market movement is going downwards, the 2 biggest SMAs are separated enough and there is a long position active or the is not an
			//active position yet.
			if (is_incipient_down_trend &&
				(Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long))
			{
				bool is_swingLow1 = false;
				bool is_swingLow2 = false;
				bool is_active_short_position = false;

				//Validate whether there is a valid swingLow1 near the SMA1 and evaluate if the swingLow1 is below the SMA2.
				if ((iSwing1.SwingLow[0] <= iSMA1[0] + (iATR[0] * ClosnessToTrade)) &&
					(iSwing1.SwingLow[0] - iSMA2[0] > iATR[0] * ClosnessFactor))
				{
					//If the Stop range contains the SMA1
					//call the Swing1 function and store the flags in the three variables below for its later use.
					if (current_stop >= iSMA1[0] - iSwing1.SwingLow[0] - distance_to_BO)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA1, false);
						is_swingLow1 = ReturnedValues.Item1;
						is_active_short_position = ReturnedValues.Item2;
						is_BO_down_swing1 = ReturnedValues.Item3;
					}
				}

				//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
				//don't evaluate this conditional statement.
				if (!is_swingLow1 && !is_active_short_position)
				{
					//Validate whether there is a valid swingLow2 near the SMA1 and evaluate if the swingLow2 is below the SMA2.
					if ((iSwing2.SwingLow[0] <= iSMA1[0] + (iATR[0] * ClosnessToTrade)) &&
						(iSwing2.SwingLow[0] - iSMA2[0] > iATR[0] * ClosnessFactor))
					{
						//If the Stop range contains the SMA1
						//call the Swing2 function and store the flags in the three variables below for its later use.
						if (current_stop >= iSMA1[0] - iSwing2.SwingLow[0] - distance_to_BO)
						{
							Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA1, false);
							is_swingLow2 = ReturnedValues.Item1;
							is_active_short_position = ReturnedValues.Item2;
							is_BO_down_swing2 = ReturnedValues.Item3;
						}
					}
				}
			}
			#endregion
			#endregion

			#region Traditional

			#region Long
			//If the overall market movement is going upwards, the 2 biggest SMAs are separated enough and there is a gray_ellipse in the current swing.
			if (is_incipient_up_trend && gray_ellipse_long)
			{
				//There is a short position active or the is not an active position yet.
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)
				{
					#region Normal_Traditional
					//If the SMA2 crossed above the SMA1.
					if (is_upward)
					{
						bool is_swingHigh1 = false;
						bool is_swingHigh2 = false;
						bool is_active_long_position = false;

						//Validate whether there is a valid swingHigh1 near the SMA2.
						if (iSwing1.SwingHigh[0] >= iSMA2[0] - (iATR[0] * ClosnessToTrade))
						{
							//If the Stop range contains the SMA2,
							//call the Swing1 function and store the flags in the three variables below for its later use.
							if (current_stop >= iSwing1.SwingHigh[0] + distance_to_BO - iSMA2[0])
							{
								Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA2, true);
								is_swingHigh1 = ReturnedValues.Item1;
								is_active_long_position = ReturnedValues.Item2;
								is_BO_up_swing1 = ReturnedValues.Item3;
							}
						}

						//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
						//don't evaluate this conditional statement.
						if (!is_swingHigh1 && !is_active_long_position)
						{
							//Validate whether there is a valid swingHigh2 near the SMA2.
							if (iSwing2.SwingHigh[0] >= iSMA2[0] - (iATR[0] * ClosnessToTrade))
							{
								//If the Stop range contains the SMA2,
								//call the Swing2 function and store the flags in the three variables below for its later use.
								if (current_stop >= iSwing2.SwingHigh[0] + distance_to_BO - iSMA2[0])
								{
									Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA2, true);
									is_swingHigh2 = ReturnedValues.Item1;
									is_active_long_position = ReturnedValues.Item2;
									is_BO_up_swing2 = ReturnedValues.Item3;
								}
							}
						}
					}
					#endregion

					#region Modified_Traditional
					//If the SMA2 crossed below the SMA1.
					else
					{
						bool is_swingHigh1 = false;
						bool is_swingHigh2 = false;
						bool is_active_long_position = false;

						//Validate whether there is a valid swingHigh1 near the SMA1.
						if (iSwing1.SwingHigh[0] >= iSMA1[0] - (iATR[0] * ClosnessToTrade))
						{
							//If the Stop range contains the SMA1,
							//call the Swing1 function and store the flags in the three variables below for its later use.
							if (current_stop >= iSwing1.SwingHigh[0] + distance_to_BO - iSMA1[0])
							{
								Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA1, true);
								is_swingHigh1 = ReturnedValues.Item1;
								is_active_long_position = ReturnedValues.Item2;
								is_BO_up_swing1 = ReturnedValues.Item3;
							}
						}

						//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
						//don't evaluate this conditional statement.
						if (!is_swingHigh1 && !is_active_long_position)
						{
							//Validate whether there is a valid swingHigh2 near the SMA1.
							if (iSwing2.SwingHigh[0] >= iSMA1[0] - (iATR[0] * ClosnessToTrade))
							{
								//If the Stop range contains the SMA1,
								//call the Swing2 function and store the flags in the three variables below for its later use.
								if (current_stop >= iSwing2.SwingHigh[0] + distance_to_BO - iSMA1[0])
								{
									Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA1, true);
									is_swingHigh2 = ReturnedValues.Item1;
									is_active_long_position = ReturnedValues.Item2;
									is_BO_up_swing2 = ReturnedValues.Item3;
								}
							}
						}
					}
					#endregion
				}
			}
			#endregion

			#region Short
			//If the overall market movement is going downwards, the 2 biggest SMAs are separated enough and there is a gray_ellipse in the current swing.
			else if (is_incipient_down_trend && gray_ellipse_short)
			{
				//There is a long position active or the is not an active position yet.
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)
				{
					#region Normal_Traditional
					//If the SMA2 crossed below the SMA1.
					if (is_downward)
					{
						bool is_swingLow1 = false;
						bool is_swingLow2 = false;
						bool is_active_short_position = false;

						//Validate whether there is a valid swingLow1 near the SMA2.
						if (iSwing1.SwingLow[0] <= iSMA2[0] + (iATR[0] * ClosnessToTrade))
						{
							//If the Stop range contains the SMA2,
							//call the Swing1 function and store the flags in the three variables below for its later use.
							if (current_stop >= iSMA2[0] - (iSwing1.SwingLow[0] - distance_to_BO))
							{
								Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA2, false);
								is_swingLow1 = ReturnedValues.Item1;
								is_active_short_position = ReturnedValues.Item2;
								is_BO_down_swing1 = ReturnedValues.Item3;
							}
						}

						//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
						//don't evaluate this conditional statement.
						if (!is_swingLow1 && !is_active_short_position)
						{
							//Validate whether there is a valid swingLow2 near the SMA2.
							if (iSwing2.SwingLow[0] <= iSMA2[0] + (iATR[0] * ClosnessToTrade))
							{
								//If the Stop range contains the SMA2,
								//call the Swing2 function and store the flags in the three variables below for its later use.
								if (current_stop >= iSMA2[0] - (iSwing2.SwingLow[0] - distance_to_BO))
								{
									Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA2, false);
									is_swingLow2 = ReturnedValues.Item1;
									is_active_short_position = ReturnedValues.Item2;
									is_BO_down_swing2 = ReturnedValues.Item3;
								}
							}
						}
					}
					#endregion

					#region Modified_Traditional
					//If the SMA2 crossed above the SMA1.
					else
					{
						bool is_swingLow1 = false;
						bool is_swingLow2 = false;
						bool is_active_short_position = false;

						//Validate whether there is a valid swingLow1 near the SMA1.
						if (iSwing1.SwingLow[0] <= iSMA1[0] + (iATR[0] * ClosnessToTrade))
						{
							//If the Stop range contains the SMA1,
							//call the Swing1 function and store the flags in the three variables below for its later use.
							if (current_stop >= iSMA1[0] - (iSwing1.SwingLow[0] - distance_to_BO))
							{
								Tuple<bool, bool, bool> ReturnedValues = Swing_1(iSMA1, false);
								is_swingLow1 = ReturnedValues.Item1;
								is_active_short_position = ReturnedValues.Item2;
								is_BO_down_swing1 = ReturnedValues.Item3;
							}
						}

						//This condition basically means that if the program entered the last conditional statement and a Swing was recognized,
						//don't evaluate this conditional statement.
						if (!is_swingLow1 && !is_active_short_position)
						{
							//Validate whether there is a valid swingLow2 near the SMA1.
							if (iSwing2.SwingLow[0] <= iSMA1[0] + (iATR[0] * ClosnessToTrade))
							{
								//If the Stop range contains the SMA1,
								//call the Swing1 function and store the flags in the three variables below for its later use.
								if (current_stop >= iSMA1[0] - (iSwing2.SwingLow[0] - distance_to_BO))
								{
									Tuple<bool, bool, bool> ReturnedValues = Swing_2(iSMA1, false);
									is_swingLow2 = ReturnedValues.Item1;
									is_active_short_position = ReturnedValues.Item2;
									is_BO_down_swing2 = ReturnedValues.Item3;
								}
							}
						}
					}
					#endregion
				}
			}
			#endregion
			#endregion
			#endregion

			#region Trade_Management
			////		TRADE MANAGEMENT (Stop and Trailing Stop Trigger Setting)						
			///Stop Updating Process (by both trailing and SMA)
			#region Long
			///While Long	
			if (!is_long &&
				Position.MarketPosition == MarketPosition.Long) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				is_long = true;
				is_short = false;
				fix_amount_long = amount_long;
				fix_stop_price_long = stop_price_long;
				fix_trigger_price_long = trigger_price_long;
				Draw.FibonacciRetracements(this, "tag1" + CurrentBar, false, 0, Position.AveragePrice, 10, fix_stop_price_long);
			}

			if (Position.MarketPosition == MarketPosition.Long && is_long) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				if (my_entry_order == null || my_entry_market == null)
				{
					if (my_entry_market == null && my_entry_order.OrderState == OrderState.Filled)
					{
						//						if (my_entry_order.OrderState == OrderState.Filled)
						//						{
						//							if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.SellShort)
						//							{
						//								Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_order.Quantity, Time[0]));
						//							}
						//							else
						//							{
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
						//							}
						//						}										
						if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] + distance_to_BO)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									HighCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}
							if (HighCrossEma50 == false)
							{
								if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryOrder");
								}
							}
							else
							{
								if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
									Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
								Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryOrder");
							}
						}
						if (High[0] >= fix_trigger_price_long)
						{
							fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
							fix_trigger_price_long = High[0];
							ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Lime);
						}
					}
					else if (my_entry_order == null && my_entry_market.OrderState == OrderState.Filled)
					{
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
						//						}
						//					}				
						if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] + distance_to_BO)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									HighCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}
							if (HighCrossEma50 == false)
							{
								if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryMarket");
								}
							}
							else
							{
								if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
									Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryMarket");
								}
							}
						}
						else
						{
							if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
								Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryMarket");
							}
						}
						if (High[0] >= fix_trigger_price_long)
						{
							fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
							fix_trigger_price_long = High[0];
							ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Lime);
						}
					}
				}
				else if (my_entry_order.OrderState == OrderState.Filled/* && my_entry_market.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_order.OrderState == OrderState.Filled && my_entry_market.OrderState != OrderState.Filled)
					//					{
					//						if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.SellShort)
					//						{
					//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_order.Quantity, Time[0]));
					//						}
					//						else
					//						{
					ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
					//						}
					//					}				
					if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] + distance_to_BO)
					{
						bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
						{
							if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								HighCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}

						if (HighCrossEma50 == false)
						{
							if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryOrder");
							}
						}
						else
						{
							if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
								Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryOrder");
							}
						}
					}
					else
					{
						if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
							Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
						{
							Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
							ExitLong(fix_amount_long, @"exit", @"entryOrder");
						}
					}

					if (High[0] >= fix_trigger_price_long)
					{
						fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
						fix_trigger_price_long = High[0];
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Lime);
					}
				}
				else if (my_entry_market.OrderState == OrderState.Filled/* && my_entry_order.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_market.OrderState == OrderState.Filled && my_entry_order.OrderState != OrderState.Filled)
					//					{
					if (my_entry_order.OrderType == OrderType.StopMarket &&
						my_entry_order.OrderState == OrderState.Working &&
						my_entry_order.OrderAction == OrderAction.SellShort)
					{
						//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_market.Quantity, Time[0]));
					}
					else
					{
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
					}
					//					}	

					if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] + distance_to_BO)
					{
						bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								HighCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}

						if (HighCrossEma50 == false)
						{
							if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryMarket");
							}
						}
						else
						{
							if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
								Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryMarket");
							}
						}
					}
					else
					{
						if (Close[0] < iSMA1[0] - (iATR[0] * (MagicNumber / 100)) ||
							Close[0] < iSMA2[0] - (iATR[0] * (MagicNumber / 100)))
						{
							Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Lime);
							ExitLong(fix_amount_long, @"exit", @"entryMarket");
						}
					}

					if (High[0] >= fix_trigger_price_long)
					{
						fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
						fix_trigger_price_long = High[0];
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Lime);
					}
				}
			}
			#endregion

			#region Short
			///While Short	
			if (!is_short &&
				Position.MarketPosition == MarketPosition.Short) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				is_long = false;
				is_short = true;
				fix_amount_short = amount_short;
				fix_stop_price_short = stop_price_short;
				fix_trigger_price_short = trigger_price_short;
				Draw.FibonacciRetracements(this, "tag1" + CurrentBar, false, 0, Position.AveragePrice, 10, fix_stop_price_short);
			}

			if (is_short &&
				Position.MarketPosition == MarketPosition.Short)
			{
				if (my_entry_order == null || my_entry_market == null)
				{
					if (my_entry_market == null && my_entry_order.OrderState == OrderState.Filled)
					{
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
						//						}
						//					}
						if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] - distance_to_BO)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									LowCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}

							if (LowCrossEma50 == false)
							{
								if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryOrder");
								}
							}
							else
							{
								if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
									Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
								Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryOrder");
							}
						}
						if (Low[0] <= fix_trigger_price_short)
						{
							fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
							fix_trigger_price_short = Low[0];
							ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Red);
						}
					}
					else if (my_entry_order == null && my_entry_market.OrderState == OrderState.Filled)
					{
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
						//						}
						//					}
						if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] - distance_to_BO)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									LowCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}

							if (LowCrossEma50 == false)
							{
								if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryMarket");
								}
							}
							else
							{
								if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
									Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryMarket");
								}
							}
						}
						else
						{
							if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
								Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryMarket");
							}
						}

						if (Low[0] <= fix_trigger_price_short)
						{
							fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
							fix_trigger_price_short = Low[0];
							ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Red);
						}
					}
				}
				else if (my_entry_order.OrderState == OrderState.Filled/* && my_entry_market.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_order.OrderState == OrderState.Filled && my_entry_market.OrderState != OrderState.Filled)
					//					{
					//						if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.Buy)
					//						{
					//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_order.Quantity, Time[0]));
					//						}
					//						else
					//						{
					ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
					//						}
					//					}
					if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] - distance_to_BO)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								LowCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}

						if (LowCrossEma50 == false)
						{
							if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryOrder");
							}
						}
						else
						{
							if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
								Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryOrder");
							}
						}
					}
					else
					{
						if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
							Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
							ExitShort(fix_amount_short, @"exit", @"entryOrder");
						}
					}
					if (Low[0] <= fix_trigger_price_short)
					{
						fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
						fix_trigger_price_short = Low[0];
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Red);
					}
				}
				else if (my_entry_market.OrderState == OrderState.Filled/* && my_entry_order.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_market.OrderState == OrderState.Filled && my_entry_order.OrderState != OrderState.Filled)
					//					{
					if (my_entry_order.OrderType == OrderType.StopMarket &&
						my_entry_order.OrderState == OrderState.Working &&
						my_entry_order.OrderAction == OrderAction.Buy)
					{
						//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_short,  my_entry_market.Quantity, Time[0]));
					}
					else
					{
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
					}
					//					}

					if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] - distance_to_BO)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								LowCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}

						if (LowCrossEma50 == false)
						{
							if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryMarket");
							}
						}
						else
						{
							if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
								Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryMarket");
							}
						}
					}
					else
					{
						if (Close[0] > iSMA1[0] + (iATR[0] * (MagicNumber / 100)) ||
							Close[0] > iSMA2[0] + (iATR[0] * (MagicNumber / 100)))
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * distance_to_BO, Brushes.Red);
							ExitShort(fix_amount_short, @"exit", @"entryMarket");
						}
					}

					if (Low[0] <= fix_trigger_price_short)
					{
						fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
						fix_trigger_price_short = Low[0];
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * distance_to_BO, Brushes.Red);
					}
				}
			}
			#endregion
			#endregion
		}

    #region Properties
    #region Indicators
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SMA1 (Max)", Order = 1, GroupName = "Indicators")]
		public int SMA1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SMA2 (Mid)", Order = 2, GroupName = "Indicators")]
		public int SMA2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SMA3 (Min)", Order = 3, GroupName = "Indicators")]
		public int SMA3
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ATR1", Order = 4, GroupName = "Indicators")]
		public int ATR1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Swing1 (Min)", Order = 5, GroupName = "Indicators")]
		public int Swing1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Swing2 (Med)", Order = 6, GroupName = "Indicators")]
		public int Swing2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Swing3 (Max)", Order = 7, GroupName = "Indicators")]
		public int Swing3
		{ get; set; }
	#endregion

	#region Parameters
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "TrailingUnitsStop", Order = 1, GroupName = "Parameters")]
		public double TrailingUnitsStop
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "UnitsTriggerForTrailing", Order = 2, GroupName = "Parameters")]
		public double UnitsTriggerForTrailing
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "RiskUnit", Order = 3, GroupName = "Parameters")]
		public int RiskUnit
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "IncipientTrandFactor", Order = 4, GroupName = "Parameters")]
		public double IncipientTrandFactor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "ATRStopFactor", Order = 5, GroupName = "Parameters")]
		public double ATRStopFactor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "TicksToBO", Order = 6, GroupName = "Parameters")]
		public double TicksToBO
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "SwingPenetration(%)", Order = 7, GroupName = "Parameters")]
		public double SwingPenetration
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "ClosnessFactor", Order = 8, GroupName = "Parameters")]
		public double ClosnessFactor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "ClosnessToTrade", Order = 9, GroupName = "Parameters")]
		public double ClosnessToTrade
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "MagicNumber(Percent (%) of ATR)", Order = 10, GroupName = "Parameters")]
		public double MagicNumber
		{ get; set; }
    #endregion

    #region Heat_Zone
		[NinjaScriptProperty]
		[Display(Name = "Days", GroupName = "Heat Zone", Order = 1)]
		public int Days
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Heat_zone_strength", GroupName = "Heat Zone", Order = 2)]
		public int Heat_zone_strength
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Width", GroupName = "Heat Zone", Order = 4)]
		public double Width
		{ get; set; }
    #endregion
    
    #region Momentum
		[NinjaScriptProperty]
		[Display(Name = "Look_back_candles", Order = 1, GroupName = "Momentum")]
		public int Look_back_candles
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Percentile", Order = 2, GroupName = "Momentum")]
		public double Percentile_v
		{ get; set; }
		#endregion
    #endregion

    #region Classes
		public class MySwing
		{
			public double value;
			public int bar;
			public bool is_broken;
		}

		public class MyHeatZone
		{
			public double value;
			public bool is_removable = false;
		}
    
		public class MyRanges
		{
			public double value;
			public double high;
			public double low;
			public int bars_ago;
			public bool is_up;
		}
	#endregion

	#region Functions
		////	METHODS			

		////	Swing Identification Method				
		public bool SwingIdentification(ISeries<double> opposite_swing, int reference_swing_bar, bool is_swingHigh)
		{
			bool is_potential_swing = true;

			if (is_swingHigh)
			{
				for (int i = 0; i <= reference_swing_bar; i++) // Walks every bar of the potential HL4
				{
					if (Close[i] < opposite_swing[i + 1] - distance_to_BO) //if there is any violation (close below a former swinglow) within the swing then...
					{
						is_potential_swing = false; //invalidates the swing high 4 (HL4)
						break;
					}

					if (Close[i] < opposite_swing[reference_swing_bar + 1] - distance_to_BO) // if there is any close below the Last opposite_swing before the current swinghigh4 reference for the entrance (the one that originated the inicial movement) then...
					{
						is_potential_swing = false; // invalidates the swing high 4 (HL4)
						break;
					}
				}
			}
			else
			{
				for (int i = 0; i <= reference_swing_bar; i++) // Walks every bar of the potential HL4
				{
					if (Close[i] > opposite_swing[i + 1] + distance_to_BO) //if there is any violation (close below a former swinglow) within the swing then...
					{
						is_potential_swing = false; //invalidates the swing high 4 (HL4)
						break;
					}

					if (Close[i] > opposite_swing[reference_swing_bar + 1] + distance_to_BO) // if there is any close below the Last opposite_swing before the current swinghigh4 reference for the entrance (the one that originated the inicial movement) then...
					{
						is_potential_swing = false; // invalidates the swing high 4 (HL4)
						break;
					}
				}
			}
			return is_potential_swing;
		}

		////	Swing Location and Characterization Method				
		public Tuple<double, double, int> SwingCharacterization(ISeries<double> bar_extreme, ISeries<double> reference_swing, int reference_swing_bar,
			bool is_swingHigh)
		{
			double swing_mid_level = reference_swing[0];
			double extreme_level = bar_extreme[0]; //initializing the variable that is going to keep the max high value of the swing, with the array firts value for comparison purposes
			int extreme_level_bar = 0;

			if (is_swingHigh)
			{
				double min_close = Close[0]; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
				for (int i = 0; i <= reference_swing_bar; i++) // Walks every bar of the potential HL4
				{
					if (Close[i] < min_close) //To determine the highest value (max close value of the swinglow4)
					{
						min_close = Close[i]; //And saves that value in this variable
					}

					if (bar_extreme[i] > extreme_level) //To determine the highest value (max close value of the swinglow4)
					{
						extreme_level = bar_extreme[i]; //And saves that value in this variable
						extreme_level_bar = i;
					}
				}
				swing_mid_level = reference_swing[0] - ((reference_swing[0] - min_close) * (SwingPenetration / 100));
			}
			else
			{
				double max_close = Close[0]; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
				for (int i = 0; i <= reference_swing_bar; i++) // Walks every bar of the potential HL4
				{
					if (Close[i] > max_close) //To determine the highest value (max close value of the swinglow4)
					{
						max_close = Close[i]; //And saves that value in this variable
					}

					if (bar_extreme[i] < extreme_level) //To determine the highest value (max close value of the swinglow4)
					{
						extreme_level = bar_extreme[i]; //And saves that value in this variable
						extreme_level_bar = i;
					}
				}
				swing_mid_level = reference_swing[0] + ((max_close - reference_swing[0]) * (SwingPenetration / 100));
			}
			return new Tuple<double, double, int>(extreme_level, swing_mid_level, extreme_level_bar);
		}

		////	Buy Process	Method		
		public bool Buy(string order_type, ISeries<double> BO_level)
		{
			if (order_type == "MarketOrder")
			{
				amount_long = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount							
				stop_price_long = Close[0] - current_stop; //calculates the stop price level
				trigger_price_long = Close[0] + current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
				EnterLong(amount_long, @"entryMarket"); // Long Stop order activation
				return true;
			}
			else if (order_type == "PendingOrder")
			{
				amount_long = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
				stop_price_long = (BO_level[0] + distance_to_BO) - current_stop; //calculates the stop price level
				trigger_price_long = (BO_level[0] + distance_to_BO) + current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger
				EnterLongStopMarket(amount_long, BO_level[0] + distance_to_BO, @"entryOrder");
				return false;
			}
			else
			{
				return false;
			}
		}

		////	Sell Process Method	 			
		public bool Sell(string order_type, ISeries<double> BO_level)
		{
			if (order_type == "MarketOrder")
			{
				amount_short = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount							
				stop_price_short = Close[0] + current_stop; //calculates the stop price level
				trigger_price_short = Close[0] - current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger
				EnterShort(amount_short, @"entryMarket"); // Long Stop order activation
				return true;
			}
			else if (order_type == "PendingOrder")
			{
				amount_short = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
				stop_price_short = (BO_level[0] - distance_to_BO) + current_stop; //calculates the stop price level
				trigger_price_short = (BO_level[0] - distance_to_BO) - current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger					
				EnterShortStopMarket(amount_short, BO_level[0] - distance_to_BO, @"entryOrder");
				return false;
			}
			else
			{
				return false;
			}
		}

		////	Swing4 operation Method			
		///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4	
		public Tuple<bool, bool, bool> Swing_1(ISeries<double> reference_SMA, bool is_high)
		{
			////		SWING 1 HIGH			
			if (is_high)
			{
				bool is_swingHigh1 = SwingIdentification(iSwing1.SwingLow, iSwing1.SwingHighBar(0, 1, CurrentBar), true);
				bool is_active_long_position = false;
				double swingHigh1_mid_level;
				int extreme_level_bar;

				if (is_swingHigh1) //if the HL4 is confirmated after the Last two validations then...
				{
					Tuple<double, double, int> ReturnedValues = SwingCharacterization(High, iSwing1.SwingHigh, iSwing1.SwingHighBar(0, 1, CurrentBar), true);
					max_high_swingHigh1 = ReturnedValues.Item1;
					swingHigh1_mid_level = ReturnedValues.Item2;
					extreme_level_bar = ReturnedValues.Item3;
					if (swingHigh1_mid_level >= reference_SMA[0])
					{
						if (iSwing2.SwingHigh[0] > iSwing1.SwingHigh[0] &&
							iSwing2.SwingHigh[0] <= iSwing1.SwingHigh[0] + (iATR[0] * ClosnessFactor))
						{
							if (current_stop >= iSwing2.SwingHigh[0] + distance_to_BO - iSMA1[0] ||
								current_stop >= iSwing2.SwingHigh[0] + distance_to_BO - iSMA2[0])
							{
								if (my_entry_order != null && my_exit_order != null)
								{
									if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) &&
										(my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) &&
										(my_entry_order.OrderAction == OrderAction.SellShort || my_exit_order.OrderAction == OrderAction.SellShort))
									{
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingHigh, iSwing1.SwingHigh, reference_SMA, extreme_level_bar, is_BO_up_swing1, true, max_high_swingHigh1);
										is_BO_up_swing1 = ReturnedValues1.Item1;
										is_active_long_position = ReturnedValues1.Item2;
									}
									else
									{
										if (max_high_swingHigh1 < iSwing2.SwingHigh[0] + distance_to_BO)
										{
											Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * distance_to_BO, Brushes.Lime); //Draw a mark to know where all conditions have been met
											is_active_long_position = Buy("PendingOrder", iSwing2.SwingHigh);

										}
										else
										{
											is_BO_up_swing1 = true;
										}
									}
								}
								else
								{
									if (max_high_swingHigh1 < iSwing2.SwingHigh[0] + distance_to_BO)
									{
										Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * distance_to_BO, Brushes.Lime); //Draw a mark to know where all conditions have been met
										is_active_long_position = Buy("PendingOrder", iSwing2.SwingHigh);

									}
									else
									{
										is_BO_up_swing1 = true;
									}
								}
							}
						}
						else
						{
							if (my_entry_order != null && my_exit_order != null)
							{
								if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) &&
									(my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) &&
									(my_entry_order.OrderAction == OrderAction.SellShort || my_exit_order.OrderAction == OrderAction.SellShort))
								{
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing1.SwingHigh, iSwing1.SwingHigh, reference_SMA, extreme_level_bar, is_BO_up_swing1, true, max_high_swingHigh1);
									is_BO_up_swing1 = ReturnedValues1.Item1;
									is_active_long_position = ReturnedValues1.Item2;
								}
								else
								{
									if (max_high_swingHigh1 < iSwing1.SwingHigh[0] + distance_to_BO)
									{
										Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing1.SwingHigh[0] + 3 * distance_to_BO, Brushes.Lime); //Draw a mark to know where all conditions have been met
										is_active_long_position = Buy("PendingOrder", iSwing1.SwingHigh);

									}
									else
									{
										is_BO_up_swing1 = true;
									}
								}
							}
							else
							{
								if (max_high_swingHigh1 < iSwing1.SwingHigh[0] + distance_to_BO)
								{
									Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing1.SwingHigh[0] + 3 * distance_to_BO, Brushes.Lime); //Draw a mark to know where all conditions have been met
									is_active_long_position = Buy("PendingOrder", iSwing1.SwingHigh);

								}
								else
								{
									is_BO_up_swing1 = true;
								}
							}
						}
					}
					else if (iSwing1.SwingHigh[0] >= reference_SMA[0])
					{
						if (iSwing2.SwingHigh[0] > iSwing1.SwingHigh[0] &&
							iSwing2.SwingHigh[0] <= iSwing1.SwingHigh[0] + (iATR[0] * ClosnessFactor))
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(iSwing2.SwingHigh, reference_SMA, max_high_swingHigh1, extreme_level_bar, 0, 1, true, is_BO_up_swing1);
							is_BO_up_swing1 = ReturnedValues1.Item1;
							is_active_long_position = ReturnedValues1.Item2;
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing_1(iSwing1.SwingHigh, reference_SMA, extreme_level_bar, 0, true);
							is_BO_up_swing1 = ReturnedValues1.Item1;
							is_active_long_position = ReturnedValues1.Item2;
						}
					}
					else
					{
						if (iSwing2.SwingHigh[0] > iSwing1.SwingHigh[0] &&
							iSwing2.SwingHigh[0] <= iSwing1.SwingHigh[0] + (iATR[0] * ClosnessFactor))
						{
							if (iSwing2.SwingHigh[0] < reference_SMA[0])
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(reference_SMA, reference_SMA, max_high_swingHigh1, extreme_level_bar, extreme_level_bar, 1, true, is_BO_up_swing1);
								is_BO_up_swing1 = ReturnedValues1.Item1;
								is_active_long_position = ReturnedValues1.Item2;
							}
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(iSwing2.SwingHigh, reference_SMA, max_high_swingHigh1, extreme_level_bar, 0, 1, true, is_BO_up_swing1);
								is_BO_up_swing1 = ReturnedValues1.Item1;
								is_active_long_position = ReturnedValues1.Item2;
							}
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing_1(reference_SMA, reference_SMA, extreme_level_bar, extreme_level_bar, true);
							is_BO_up_swing1 = ReturnedValues1.Item1;
							is_active_long_position = ReturnedValues1.Item2;
						}
					}
				}
				if (is_BO_up_swing1)
				{
					is_swingHigh1 = false;
				}
				return new Tuple<bool, bool, bool>(is_swingHigh1, is_active_long_position, is_BO_up_swing1);
			}

			////		SWING 1 LOW
			else
			{
				bool is_swingLow1 = SwingIdentification(iSwing1.SwingHigh, iSwing1.SwingLowBar(0, 1, CurrentBar), false);
				bool is_active_short_position = false;
				double swingLow1_mid_level;
				int extreme_level_bar;

				if (is_swingLow1)
				{
					Tuple<double, double, int> ReturnedValues = SwingCharacterization(Low, iSwing1.SwingLow, iSwing1.SwingLowBar(0, 1, CurrentBar), false);
					min_low_swingLow1 = ReturnedValues.Item1;
					swingLow1_mid_level = ReturnedValues.Item2;
					extreme_level_bar = ReturnedValues.Item3;
					if (swingLow1_mid_level <= reference_SMA[0] /*&& min_low_swingLow1 > iSwing1.SwingLow[0] - distance_to_BO*/)
					{
						if (iSwing2.SwingLow[0] < iSwing1.SwingLow[0] &&
							iSwing2.SwingLow[0] >= iSwing1.SwingLow[0] - (iATR[0] * ClosnessFactor))
						{
							if (current_stop >= iSMA1[0] - iSwing2.SwingLow[0] - distance_to_BO ||
								current_stop >= iSMA2[0] - iSwing2.SwingLow[0] - distance_to_BO)
							{
								if (my_entry_order != null && my_exit_order != null)
								{
									if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) &&
										(my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) &&
										(my_entry_order.OrderAction == OrderAction.Buy || my_exit_order.OrderAction == OrderAction.Buy))
									{
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingLow, iSwing1.SwingLow, reference_SMA, extreme_level_bar, is_BO_down_swing1, false, min_low_swingLow1);
										is_BO_down_swing1 = ReturnedValues1.Item1;
										is_active_short_position = ReturnedValues1.Item2;
									}
									else
									{
										if (min_low_swingLow1 > iSwing2.SwingLow[0] - distance_to_BO)
										{
											Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * distance_to_BO, Brushes.Red);
											is_active_short_position = Sell("PendingOrder", iSwing2.SwingLow);
										}
										else
										{
											is_BO_down_swing1 = true;
										}
									}
								}
								else
								{
									if (min_low_swingLow1 > iSwing2.SwingLow[0] - distance_to_BO)
									{
										Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * distance_to_BO, Brushes.Red);
										is_active_short_position = Sell("PendingOrder", iSwing2.SwingLow);
									}
									else
									{
										is_BO_down_swing1 = true;
									}
								}
							}
						}
						else
						{
							if (my_entry_order != null && my_exit_order != null)
							{
								if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) &&
									(my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) &&
									(my_entry_order.OrderAction == OrderAction.Buy || my_exit_order.OrderAction == OrderAction.Buy))
								{
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing1.SwingLow, iSwing1.SwingLow, reference_SMA, extreme_level_bar, is_BO_down_swing1, false, min_low_swingLow1);
									is_BO_down_swing1 = ReturnedValues1.Item1;
									is_active_short_position = ReturnedValues1.Item2;
								}
								else
								{
									if (min_low_swingLow1 > iSwing1.SwingLow[0] - distance_to_BO)
									{
										Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing1.SwingLow[0] - 3 * distance_to_BO, Brushes.Red);
										is_active_short_position = Sell("PendingOrder", iSwing1.SwingLow);
									}
									else
									{
										is_BO_down_swing1 = true;
									}
								}
							}
							else
							{
								if (min_low_swingLow1 > iSwing1.SwingLow[0] - distance_to_BO)
								{
									Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing1.SwingLow[0] - 3 * distance_to_BO, Brushes.Red);
									is_active_short_position = Sell("PendingOrder", iSwing1.SwingLow);
								}
								else
								{
									is_BO_down_swing1 = true;
								}
							}
						}
					}
					else if (iSwing1.SwingLow[0] <= reference_SMA[0])
					{
						if (iSwing2.SwingLow[0] < iSwing1.SwingLow[0] &&
							iSwing2.SwingLow[0] >= iSwing1.SwingLow[0] - (iATR[0] * ClosnessFactor))
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(iSwing2.SwingLow, reference_SMA, min_low_swingLow1, extreme_level_bar, 0, 1, false, is_BO_down_swing1);
							is_BO_down_swing1 = ReturnedValues1.Item1;
							is_active_short_position = ReturnedValues1.Item2;
						}
						else
						{
							//							Print (String.Format("{0} // {1} // {2} // {3}", is_BO_down_swing1, iSwing2.SwingLow[0], iSwing1.SwingLow[0], Time[0]));
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing_1(iSwing1.SwingLow, reference_SMA, extreme_level_bar, 0, false);
							is_BO_down_swing1 = ReturnedValues1.Item1;
							is_active_short_position = ReturnedValues1.Item2;
						}
					}
					else
					{
						if (iSwing2.SwingLow[0] < iSwing1.SwingLow[0] &&
							iSwing2.SwingLow[0] >= iSwing1.SwingLow[0] - (iATR[0] * ClosnessFactor))
						{
							if (iSwing2.SwingLow[0] > reference_SMA[0])
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(reference_SMA, reference_SMA, min_low_swingLow1, extreme_level_bar, extreme_level_bar, 1, false, is_BO_down_swing1);
								is_BO_down_swing1 = ReturnedValues1.Item1;
								is_active_short_position = ReturnedValues1.Item2;
							}
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(iSwing2.SwingLow, reference_SMA, min_low_swingLow1, extreme_level_bar, 0, 1, false, is_BO_down_swing1);
								is_BO_down_swing1 = ReturnedValues1.Item1;
								is_active_short_position = ReturnedValues1.Item2;
							}
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing_1(reference_SMA, reference_SMA, extreme_level_bar, extreme_level_bar, false);
							is_BO_down_swing1 = ReturnedValues1.Item1;
							is_active_short_position = ReturnedValues1.Item2;
						}
					}
				}
				if (is_BO_down_swing1)
				{
					is_swingLow1 = false;
				}
				return new Tuple<bool, bool, bool>(is_swingLow1, is_active_short_position, is_BO_down_swing1);
			}
		}

		////	Swing14 operation Method				
		public Tuple<bool, bool, bool> Swing_2(ISeries<double> reference_SMA, bool is_high)
		{
			////		SWING 2 HIGH
			if (is_high)
			{
				bool is_swingHigh2 = SwingIdentification(iSwing2.SwingLow, iSwing2.SwingHighBar(0, 1, CurrentBar), true);
				bool is_active_long_position = false;
				double swingHigh2_mid_level;
				int extreme_level_bar;

				if (is_swingHigh2)
				{
					Tuple<double, double, int> ReturnedValues = SwingCharacterization(High, iSwing2.SwingHigh, iSwing2.SwingHighBar(0, 1, CurrentBar), true);
					max_high_swingHigh2 = ReturnedValues.Item1;
					swingHigh2_mid_level = ReturnedValues.Item2;
					extreme_level_bar = ReturnedValues.Item3;
					if (swingHigh2_mid_level >= reference_SMA[0]  /*&& max_high_swingHigh2 < iSwing2.SwingHigh[0] + distance_to_BO*/)
					{
						if (my_entry_order != null && my_exit_order != null)
						{
							if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) &&
								(my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) &&
								(my_entry_order.OrderAction == OrderAction.SellShort || my_exit_order.OrderAction == OrderAction.SellShort))
							{
								Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingHigh, iSwing2.SwingHigh, reference_SMA, extreme_level_bar, is_BO_up_swing2, true, max_high_swingHigh2);
								is_BO_up_swing2 = ReturnedValues1.Item1;
								is_active_long_position = ReturnedValues1.Item2;
							}
							else
							{

								if (max_high_swingHigh2 < iSwing2.SwingHigh[0] + distance_to_BO)
								{
									Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * distance_to_BO, Brushes.Green);
									is_active_long_position = Buy("PendingOrder", iSwing2.SwingHigh);
								}
								else
								{
									is_BO_up_swing2 = true;
								}
							}
						}
						else
						{
							if (max_high_swingHigh2 < iSwing2.SwingHigh[0] + distance_to_BO)
							{
								Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * distance_to_BO, Brushes.Green);
								is_active_long_position = Buy("PendingOrder", iSwing2.SwingHigh);
							}
							else
							{
								is_BO_up_swing2 = true;
							}
						}
					}
					else if (iSwing2.SwingHigh[0] >= reference_SMA[0])
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(iSwing2.SwingHigh, reference_SMA, max_high_swingHigh2, extreme_level_bar, 0, 2, true, is_BO_up_swing2);
						is_BO_up_swing2 = ReturnedValues1.Item1;
						is_active_long_position = ReturnedValues1.Item2;
					}
					else
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(reference_SMA, reference_SMA, max_high_swingHigh2, extreme_level_bar, extreme_level_bar, 2, true, is_BO_up_swing2);
						is_BO_up_swing2 = ReturnedValues1.Item1;
						is_active_long_position = ReturnedValues1.Item2;
					}
				}
				if (is_BO_up_swing2)
				{
					is_swingHigh2 = false;
				}
				return new Tuple<bool, bool, bool>(is_swingHigh2, is_active_long_position, is_BO_up_swing2);
			}

			////		SWING 2 LOW
			else
			{
				bool is_swingLow2 = SwingIdentification(iSwing2.SwingHigh, iSwing2.SwingLowBar(0, 1, CurrentBar), false);
				bool is_active_short_position = false;
				double swingLow2_mid_level;
				int extreme_level_bar;

				if (is_swingLow2)
				{
					Tuple<double, double, int> ReturnedValues = SwingCharacterization(Low, iSwing2.SwingLow, iSwing2.SwingLowBar(0, 1, CurrentBar), false);
					min_low_swingLow2 = ReturnedValues.Item1;
					swingLow2_mid_level = ReturnedValues.Item2;
					extreme_level_bar = ReturnedValues.Item3;
					if (swingLow2_mid_level <= reference_SMA[0] /*&& min_low_swingLow2 > iSwing2.SwingLow[0] - distance_to_BO*/)
					{
						if (my_entry_order != null && my_exit_order != null)
						{
							if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) &&
								(my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) &&
								(my_entry_order.OrderAction == OrderAction.Buy || my_exit_order.OrderAction == OrderAction.Buy))
							{
								Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingLow, iSwing2.SwingLow, reference_SMA, extreme_level_bar, is_BO_down_swing2, false, min_low_swingLow2);
								is_BO_down_swing2 = ReturnedValues1.Item1;
								is_active_short_position = ReturnedValues1.Item2;
							}
							else
							{
								if (min_low_swingLow2 > iSwing2.SwingLow[0] - distance_to_BO)
								{
									Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * distance_to_BO, Brushes.Maroon);
									is_active_short_position = Sell("PendingOrder", iSwing2.SwingLow);
								}
								else
								{
									is_BO_down_swing2 = true;
								}
							}
						}
						else
						{
							if (min_low_swingLow2 > iSwing2.SwingLow[0] - distance_to_BO)
							{
								Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * distance_to_BO, Brushes.Maroon);
								is_active_short_position = Sell("PendingOrder", iSwing2.SwingLow);
							}
							else
							{
								is_BO_down_swing2 = true;
							}
						}
					}
					else if (iSwing2.SwingLow[0] <= reference_SMA[0])
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(iSwing2.SwingLow, reference_SMA, min_low_swingLow2, extreme_level_bar, 0, 2, false, is_BO_down_swing2);
						is_BO_down_swing2 = ReturnedValues1.Item1;
						is_active_short_position = ReturnedValues1.Item2;
					}
					else
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing_2(reference_SMA, reference_SMA, min_low_swingLow2, extreme_level_bar, extreme_level_bar, 2, false, is_BO_down_swing2);
						is_BO_down_swing2 = ReturnedValues1.Item1;
						is_active_short_position = ReturnedValues1.Item2;
					}
				}
				if (is_BO_down_swing2)
				{
					is_swingLow2 = false;
				}
				return new Tuple<bool, bool, bool>(is_swingLow2, is_active_short_position, is_BO_down_swing2);
			}
		}

		////	BO Swing4 proof when market order oportunity				
		public Tuple<bool, bool> BOProofSwing_1(ISeries<double> reference_BO_level, ISeries<double> reference_SMA, int reference_bar, int reference_SMA_BO_bar,
			bool is_up)
		{
			////		UP
			bool is_active_long_position = false;
			bool is_active_short_position = false;

			if (is_up)
			{
				if (!is_BO_up_swing1)
				{
					if (High[0] >= reference_BO_level[0] + distance_to_BO)
					{
						if (reference_bar == 0)
						{
							Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, reference_BO_level[0] + 3 * distance_to_BO, Brushes.Lime);
							is_BO_up_swing1 = true;
							if (Close[0] >= reference_BO_level[0] + distance_to_BO)
							{
								if (Close[0] > iSwing2.SwingHigh[0] + distance_to_BO)
								{
									if (current_stop >= Close[0] - reference_SMA[0])
									{
										is_active_long_position = Buy("MarketOrder", Close);
									}
								}
								else
								{
									if (current_stop >= Close[0] - reference_SMA[0] &&
										iSwing2.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor)
									{
										is_active_long_position = Buy("MarketOrder", Close);
									}
								}
							}
						}
						else
						{
							is_BO_up_swing1 = true;
						}
					}
					else
					{
						if (max_high_swingHigh1 <= reference_BO_level[reference_SMA_BO_bar])
						{
							if (max_high_swingHigh1 >= reference_BO_level[reference_SMA_BO_bar] + distance_to_BO)
							{
								is_BO_up_swing1 = true;
							}
						}
						else
						{
							if (max_high_swingHigh1 >= reference_BO_level[0] + distance_to_BO)
							{
								is_BO_up_swing1 = true;
							}
						}
					}

					if (!is_BO_up_swing1)
					{
						Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, reference_BO_level[0] + 3 * distance_to_BO, Brushes.Lime);
					}
				}
				else
				{
					if (max_high_swingHigh1 <= iSwing1.SwingHigh[0])
					{
						is_BO_up_swing1 = false;
					}
					else if (Close[0] >= last_max_high_swingHigh1 + distance_to_BO && reference_bar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh1 + 3 * distance_to_BO, Brushes.Cyan);
						if (Close[0] > iSwing2.SwingHigh[0] + distance_to_BO)
						{
							if (current_stop >= Close[0] - reference_SMA[0])
							{
								is_active_long_position = Buy("MarketOrder", Close);
							}
						}
						else
						{
							if (current_stop >= Close[0] - reference_SMA[0] &&
								iSwing2.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor)
							{
								is_active_long_position = Buy("MarketOrder", Close);
							}
						}
					}
				}
				return new Tuple<bool, bool>(is_BO_up_swing1, is_active_long_position);
			}
			else
			{
				if (!is_BO_down_swing1)
				{
					if (Low[0] <= reference_BO_level[0] - distance_to_BO)
					{
						if (reference_bar == 0)
						{
							Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, reference_BO_level[0] - 3 * distance_to_BO, Brushes.Red);
							is_BO_down_swing1 = true;
							if (Close[0] <= reference_BO_level[0] - distance_to_BO)
							{
								if (iSwing2.SwingLow[0] > Close[0])
								{
									if (current_stop >= reference_SMA[0] - Close[0])
									{
										is_active_short_position = Sell("MarketOrder", Close);
									}
								}
								else
								{
									if (current_stop >= reference_SMA[0] - Close[0] &&
										Close[0] - iSwing2.SwingLow[0] > iATR[0] * ClosnessFactor)
									{
										is_active_short_position = Sell("MarketOrder", Close);
									}
								}
							}
						}
						else
						{
							is_BO_down_swing1 = true;
						}
					}
					else
					{
						if (min_low_swingLow1 >= reference_BO_level[reference_SMA_BO_bar])
						{
							if (min_low_swingLow1 <= reference_BO_level[reference_SMA_BO_bar] - distance_to_BO)
							{
								is_BO_down_swing1 = true;
							}
						}
						else
						{
							if (min_low_swingLow1 <= reference_BO_level[0] - distance_to_BO)
							{
								is_BO_down_swing1 = true;
							}
						}
					}

					if (!is_BO_down_swing1)
					{
						Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, reference_BO_level[0] - 3 * distance_to_BO, Brushes.Red);
					}
				}
				else
				{
					if (min_low_swingLow1 >= iSwing1.SwingLow[0])
					{
						is_BO_down_swing1 = false;
					}
					else if (Close[0] <= last_min_low_swingLow1 - distance_to_BO && reference_bar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow1 - 3 * distance_to_BO, Brushes.Indigo);
						if (iSwing2.SwingLow[0] > Close[0])
						{
							if (current_stop >= reference_SMA[0] - Close[0])
							{
								is_active_short_position = Sell("MarketOrder", Close);
							}
						}
						else
						{
							if (current_stop >= reference_SMA[0] - Close[0] &&
								Close[0] - iSwing2.SwingLow[0] > iATR[0] * ClosnessFactor)
							{
								is_active_short_position = Sell("MarketOrder", Close);
							}
						}
					}
				}
				return new Tuple<bool, bool>(is_BO_down_swing1, is_active_short_position);
			}
		}

		////	BOSwing14 proof when market order oportunity				
		public Tuple<bool, bool> BOProofSwing_2(ISeries<double> reference_BO_level, ISeries<double> reference_SMA, double extreme_level, int reference_bar,
			int reference_SMA_BO_bar, int swing, bool is_up, bool is_BO)
		{
			bool is_active_long_position = false;
			bool is_active_short_position = false;

			if (is_up)
			{
				if (!is_BO)
				{
					if (High[0] >= reference_BO_level[0] + distance_to_BO)
					{
						if (reference_bar == 0)
						{
							Draw.Square(this, @"GreenSquare" + CurrentBar, true, 0, reference_BO_level[0] + 3 * distance_to_BO, Brushes.Green);
							is_BO = true;

							if (Close[0] >= reference_BO_level[0] + distance_to_BO)
							{
								if (current_stop >= Close[0] - reference_SMA[0])
								{
									is_active_long_position = Buy("MarketOrder", Close);
								}
							}
						}
						else
						{
							is_BO = true;
						}
					}
					else
					{
						if (extreme_level <= reference_BO_level[reference_SMA_BO_bar])
						{
							if (extreme_level >= reference_BO_level[reference_SMA_BO_bar] + distance_to_BO)
							{
								is_BO = true;
							}
						}
						else
						{
							if (extreme_level >= reference_BO_level[0] + distance_to_BO)
							{
								is_BO = true;
							}
						}
					}

					if (!is_BO)
					{
						Draw.Square(this, @"GreenSquare" + CurrentBar, true, 0, reference_BO_level[0] + 3 * distance_to_BO, Brushes.Green);
					}
				}
				else
				{
					if (extreme_level <= iSwing2.SwingHigh[0])
					{
						is_BO = false;
					}
					else
					{
						if (swing == 1)
						{
							if (Close[0] >= last_max_high_swingHigh1 + distance_to_BO)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh1 + 3 * distance_to_BO, Brushes.Cyan);

								if (current_stop >= Close[0] - reference_SMA[0])
								{
									is_active_long_position = Buy("MarketOrder", Close);
								}
							}
						}
						else if (swing == 2)
						{
							if (Close[0] >= last_max_high_swingHigh2 + distance_to_BO && reference_bar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh2 + 3 * distance_to_BO, Brushes.Cyan);

								if (current_stop >= Close[0] - reference_SMA[0])
								{
									is_active_long_position = Buy("MarketOrder", Close);
								}
							}
						}
					}
				}
				return new Tuple<bool, bool>(is_BO, is_active_long_position);
			}
			else
			{
				if (!is_BO)
				{
					if (Low[0] <= reference_BO_level[0] - distance_to_BO)
					{
						if (reference_bar == 0)
						{
							Draw.Square(this, @"MaroonSquare" + CurrentBar, true, 0, reference_BO_level[0] - 3 * distance_to_BO, Brushes.Maroon);
							is_BO = true;

							if (Close[0] <= reference_BO_level[0] - distance_to_BO)
							{
								if (current_stop >= reference_SMA[0] - Close[0])
								{
									is_active_short_position = Sell("MarketOrder", Close);
								}
							}
						}
						else
						{
							is_BO = true;
						}
					}
					else
					{
						if (extreme_level >= reference_BO_level[reference_SMA_BO_bar])
						{
							if (extreme_level <= reference_BO_level[reference_SMA_BO_bar] - distance_to_BO)
							{
								is_BO = true;
							}
						}
						else
						{
							if (extreme_level <= reference_BO_level[0] - distance_to_BO)
							{
								is_BO = true;
							}
						}
					}

					if (!is_BO)
					{
						Draw.Square(this, @"MaroonSquare" + CurrentBar, true, 0, reference_BO_level[0] - 3 * distance_to_BO, Brushes.Maroon);
					}
				}
				else
				{
					if (extreme_level >= iSwing2.SwingLow[0])
					{
						is_BO = false;
					}
					else
					{
						if (swing == 1)
						{
							if (Close[0] <= last_min_low_swingLow1 - distance_to_BO)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow1 - 3 * distance_to_BO, Brushes.Cyan);

								if (current_stop >= reference_SMA[0] - Close[0])
								{
									is_active_short_position = Sell("MarketOrder", Close);
								}
							}
						}
						else if (swing == 2)
						{
							if (Close[0] <= last_min_low_swingLow2 - distance_to_BO && reference_bar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow2 - 3 * distance_to_BO, Brushes.Cyan);

								if (current_stop >= reference_SMA[0] - Close[0])
								{
									is_active_short_position = Sell("MarketOrder", Close);
								}
							}
						}
					}
				}
				return new Tuple<bool, bool>(is_BO, is_active_short_position);
			}
		}

		////	Market order submission when there is an opposing peinding order				
		public Tuple<bool, bool> MarketVSPending(ISeries<double> reference_BO_level, ISeries<double> reference_swing, ISeries<double> reference_SMA, int reference_bar,
			bool is_BO, bool is_up, double extreme_level)
		{
			bool isActiveLongPosition = false;
			bool isActiveShortPosition = false;

			if (is_up)
			{
				if (!is_BO)
				{
					if (High[0] >= reference_BO_level[0] + distance_to_BO)
					{
						if (reference_bar == 0)
						{
							Draw.Square(this, @"WhiteSquare" + CurrentBar, true, 0, reference_BO_level[0] + 3 * distance_to_BO, Brushes.White);
							is_BO = true;

							if (current_stop >= Close[0] - reference_SMA[0])
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}
						else
						{
							is_BO = true;
						}
					}
					else
					{
						if (extreme_level >= reference_BO_level[0] + distance_to_BO)
						{
							is_BO = true;
						}
					}

					if (!is_BO)
					{
						Draw.Square(this, @"WhiteSquare" + CurrentBar, true, 0, reference_BO_level[0] + 3 * distance_to_BO, Brushes.White);
					}
				}
				else
				{
					if (extreme_level <= reference_swing[0])
					{
						is_BO = false;
					}
				}
				return new Tuple<bool, bool>(is_BO, isActiveLongPosition);
			}
			else
			{
				if (!is_BO)
				{
					if (Low[0] <= reference_BO_level[0] - distance_to_BO)
					{
						if (reference_bar == 0)
						{
							Draw.Square(this, @"BlackSquare" + CurrentBar, true, 0, reference_BO_level[0] - 3 * distance_to_BO, Brushes.Black);
							is_BO = true;

							if (current_stop >= reference_SMA[0] - Close[0])
							{
								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
						else
						{
							is_BO = true;
						}
					}
					else
					{
						if (extreme_level <= reference_BO_level[0] - distance_to_BO)
						{
							is_BO = true;
						}
					}

					if (!is_BO)
					{
						Draw.Square(this, @"BlackSquare" + CurrentBar, true, 0, reference_BO_level[0] - 3 * distance_to_BO, Brushes.Black);
					}
				}
				else
				{
					if (extreme_level >= reference_swing[0])
					{
						is_BO = false;
					}
				}
				return new Tuple<bool, bool>(is_BO, isActiveShortPosition);
			}
		}

		///<summary>
		///Finds the value in the sequence, equivalent to the Percentile given.
		///</summary>
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
	#endregion

		public void MyPrint()
        {
			Print(Time[0]);
			Print("SwingHigh3");
			for (int i = 0; i < swings3_high.Count; i++)
			{
				Print(string.Format("Value: {0} // is_broken: {1} // bar: {2}", swings3_high[i].value, swings3_high[i].is_broken, swings3_high[i].bar));
			}
			Print("-------");
			Print("SwingLow3");
			for (int i = 0; i < swings3_low.Count; i++)
			{
				Print(string.Format("Value: {0} // is_broken: {1} // bar: {2}", swings3_low[i].value, swings3_low[i].is_broken, swings3_low[i].bar));
			}
			Print("-------");
			Print("SwingHigh2");
			for (int i = 0; i < swings2_high.Count; i++)
			{
				Print(string.Format("Value: {0} // is_broken: {1} // bar: {2}", swings2_high[i].value, swings2_high[i].is_broken, swings2_high[i].bar));
			}
			Print("-------");
			Print("SwingLow2");
			for (int i = 0; i < swings2_low.Count; i++)
			{
				Print(string.Format("Value: {0} // is_broken: {1} // bar: {2}", swings2_low[i].value, swings2_low[i].is_broken, swings2_low[i].bar));
			}
			Print("---------------------------------------");
		}
	}
}