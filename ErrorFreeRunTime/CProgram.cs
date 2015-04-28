using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorFreeRunTime
{
	public class CProgram
	{
		private struct SRunTimeStatistics
		{
			public readonly UInt64 m_cycle_count;
			public readonly Double m_run_time_in_ms;

			public SRunTimeStatistics(UInt64 cycle_count, Double run_time)
			{
				m_cycle_count = cycle_count;
				m_run_time_in_ms = run_time;
			}
		}

		public static void Main(String[] p_args)
		{
			try
			{
				if (p_args == null || p_args.Length == 0)
				{
					// usage text here
					Console.WriteLine(
	@"
	ErrorFree Language Runtime Program

	Usage:		efrt [file] [options]

	Options:

	/a0				This will make the standard-input of the
					ErrorFree program see a constant stream of
					ASCII 0s (ie 0x30 bytes).

	/rnd=length		This option will, instead of executing any
					files, generate a random byte array with
					the specified length and execute it.

	Will interpret the given file as an ErrorFree
	Program and run it.
"
	);
				}
				else
				{
					Boolean auto_zero = false;
					Byte[] p_rnd = null;
					List<String> p_files = new List<String>();
					for (Int32 i = 0; i < p_args.Length; ++i)
					{
						if (p_args[i].StartsWith("/a0"))
						{
							auto_zero = true;
						}
						else if (p_args[i].StartsWith("/rnd="))
						{
							if (p_rnd != null)
							{
								Console.WriteLine("EFI: You cannot have more than one rnd parameter.");
								return;
							}
							String p_str = p_args[i].Substring(5);
							Int32 len;
							if (!Int32.TryParse(p_str, out len))
							{
								Console.WriteLine("EFI: The given length for the rnd parameter was not an integer.");
								return;
							}
							if (len < 0)
							{
								Console.WriteLine("EFI: The given length for the rnd parameter was negative.");
								return;
							}
							p_rnd = new Byte[len];
							new Random().NextBytes(p_rnd);
						}
						else
						{
							p_files.Add(p_args[i]);
						}
					}
					if (p_rnd != null)
					{
						Console.Title = "Random Bytes: ";
						RunBytes(p_rnd, auto_zero);
						return;
					}
					for (Int32 i = 0; i < p_files.Count; ++i)
					{
						RunFile(p_files[i], auto_zero);
					}
				}
			}
			catch (Exception p_except)
			{
				Console.WriteLine("Error occured: " + p_except.ToString());
				Console.ReadLine();
			}
		}


		private static void RunFile(String p_path, Boolean auto_zero)
		{
			if (p_path == null)
				return;

			Console.WriteLine("EFI: Running file \"" + Path.GetFileName(p_path) + "\".");
			if (!File.Exists(p_path))
			{
				Console.WriteLine("EFI: File \"" + p_path + "\" could not be found.");
			}

			Byte[] p_bytes;
			try
			{
				p_bytes = File.ReadAllBytes(p_path);
			}
			catch (Exception p_except)
			{
				Console.WriteLine("EFI: Error occured while trying to read file \"" + Path.GetFileName(p_path) + "\".");
				Console.WriteLine(p_except.ToString());
				return;
			}

			Console.Title = Path.GetFileName(p_path) + ": ";
			SRunTimeStatistics stats = RunBytes(p_bytes, auto_zero);
			Console.WriteLine("EFI: Finished running file \"" + Path.GetFileName(p_path) + "\".");
		}
		private static SRunTimeStatistics RunBytes(Byte[] p_bytes, Boolean auto_zero)
		{
			Int32 initial_title_length = Console.Title.Length;
			Random p_rng = new Random();
			CErrorFreeStack<Double> p_stack = new CErrorFreeStack<Double>();
			CErrorFreeHeap<Double> p_heap = new CErrorFreeHeap<Double>();

			UInt64 cycle_count = 0;
			Stopwatch p_sw = Stopwatch.StartNew();
			p_sw.Reset();
			TimeSpan last_title_update = new TimeSpan(0L);
			for (Int32 i = 0; i < p_bytes.Length; ++i)
			{
				if ((p_sw.Elapsed - last_title_update).TotalSeconds > 0.5)
				{
					Console.Title = Console.Title.Substring(0, initial_title_length) + i.ToString() + " / " + p_bytes.Length.ToString() + ", " + cycle_count.ToString() + " Cycles, Byte " + (Char)p_bytes[i];
					last_title_update = p_sw.Elapsed;
				}
				++cycle_count;
				p_sw.Start();
				switch (p_bytes[i])
				{
						// Arithmetic Operators
					case (Byte)'+': // push sum of last two stack values
						p_stack.Push(p_stack.Pop() + p_stack.Pop());
						break;
					case (Byte)'-': // push difference of last two stack values
						p_stack.Push(p_stack.Pop() - p_stack.Pop());
						break;
					case (Byte)'*': // push multiple of last two stack values
						p_stack.Push(p_stack.Pop() * p_stack.Pop());
						break;
					case (Byte)'/': // push division of last two stack values
						Double base_div = p_stack.Pop();
						Double divisor = p_stack.Pop();
						if (divisor == 0.0)
						{
							if (base_div == 0.0)
								p_stack.Push(Double.NaN);
							else if (base_div < 0.0)
								p_stack.Push(Double.NegativeInfinity);
							else
								p_stack.Push(Double.PositiveInfinity);
						}
						else
						{
							p_stack.Push(base_div / divisor);
						}
						break;
					case (Byte)'%': // push modulo of last two stack values
						p_stack.Push(p_stack.Pop() % p_stack.Pop()); // this is inconsistent with the way division behaves
						break;
					case (Byte)'^': // push second-to-last stack value to the power of the last stack value
						p_stack.Push(Math.Pow(p_stack.Pop(), p_stack.Pop())); // all hail the C# specification specifying order of evaluation for parameters to allow this one-liner.
						break;
					case (Byte)'=': // push one if last two stack values are equal, zero otherwise (NaN != NaN)
						if (p_stack.Pop() == p_stack.Pop())
							p_stack.Push(1.0);
						else
							p_stack.Push(0.0);
						break;
					case (Byte)'>': // push one if second-to-last stack value > last stack value, zero otherwise
						if (p_stack.Pop() < p_stack.Pop())
							p_stack.Push(1.0);
						else
							p_stack.Push(0.0);
						break;
					case (Byte)'<': // push one if second-to-last stack value < last stack value, zero otherwise:
						if (p_stack.Pop() > p_stack.Pop())
							p_stack.Push(1.0);
						else
							p_stack.Push(0.0);
						break;

						// Pure Operators
					case (Byte)'d':
						p_stack.Push(p_stack.Peek());
						break;
					case (Byte)'t':
						Double top_most = p_stack.Pop();
						Double second_to_last = p_stack.Pop();
						p_stack.Push(top_most);
						p_stack.Push(second_to_last);
						break;
					case (Byte)'a':
						p_stack.Push(Math.Abs(p_stack.Pop()));
						break;
					case (Byte)'s':
						Double sign_val = p_stack.Pop();
						if (Double.IsNaN(sign_val))
							p_stack.Push(Double.NaN);
						else if (sign_val < 0.0)
							p_stack.Push(-1.0);
						else if (sign_val > 0.0)
							p_stack.Push(1.0);
						else if (sign_val == 0.0)
							p_stack.Push(0.0);
						break;
					case (Byte)'r':
						Double sqrt_val = p_stack.Pop();
						if (sqrt_val < 0.0)
							p_stack.Push(Double.NaN);
						else
							p_stack.Push(Math.Sqrt(sqrt_val));
						break;
					case (Byte)'l':
						Double log10_val = p_stack.Pop();
						if (log10_val < 0.0)
							p_stack.Push(Double.NaN);
						else
							p_stack.Push(Math.Log10(log10_val));
						break;
					case (Byte)'f':
						p_stack.Push(Math.Floor(p_stack.Pop()));
						break;
					case (Byte)'c':
						p_stack.Push(Math.Ceiling(p_stack.Pop()));
						break;

						// Impure Operators
					case (Byte)'C':
						Double print_val = p_stack.Pop();
						if (Double.IsNaN(print_val) || Double.IsInfinity(print_val))
							print_val = 0.0;
						else if (print_val < 0.0)
							print_val *= -1.0;

						Console.Write((Char)(UInt16)print_val);
						break;
					case (Byte)'N':
						Console.WriteLine(p_stack.Pop());
						break;
					case (Byte)'D':
						if (auto_zero)
							p_stack.Push(0.0);
						else
							p_stack.Push(Console.Read());
						break;
					case (Byte)'O':
						if (auto_zero)
							p_stack.Push(0.0);
						else
							p_stack.Push(ErrorFreeDoubleParse(Console.ReadLine()));
						break;
					case (Byte)'R':
						p_stack.Push(p_rng.NextDouble());
						break;
					case (Byte)'T':
						p_stack.Push((Int64)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
						break;
					case (Byte)'J':
						Double jump_offset = p_stack.Pop();
						if (Double.IsNaN(jump_offset) || Double.IsInfinity(jump_offset))
							jump_offset = 0.0;

						Double target_pos = (Double)i + jump_offset;
						if (target_pos < 0.0)
						{
							Double num_boundaries = Math.Ceiling(Math.Abs(target_pos) / p_bytes.Length);
							target_pos += num_boundaries * (Double)p_bytes.Length;
							i = (Int32)target_pos;
						}
						else if (target_pos >= p_bytes.Length)
						{
							Double num_boundaries = Math.Floor(target_pos / p_bytes.Length);
							target_pos -= num_boundaries * (Double)p_bytes.Length;
							i = (Int32)target_pos;
						}
						if(jump_offset != 0)
							--i; // make sure we don't skip a byte via ++i as the loop increment if we actually do jump
						break;
					case (Byte)'S':
						Double first = p_stack.Pop();
						Double second = p_stack.Pop();
						p_heap.WriteElement(first, second);
						break;
					case (Byte)'L':
						p_stack.Push(p_heap.ReadElement(p_stack.Pop()));
						break;
						
						// Newlines are meaningless
					case (Byte)'\n':
					case (Byte)'\r':
						break;

					default:
						p_stack.Push(p_bytes[i]);
						break;
				}
				p_sw.Stop();
			}

			Console.WriteLine("EFI: Executed {0} bytes ({1} cycles) in {2} ms", p_bytes.Length, cycle_count, p_sw.Elapsed.TotalMilliseconds);
			return new SRunTimeStatistics(cycle_count, p_sw.Elapsed.TotalMilliseconds);
		}

		static CultureInfo p_en_culture = new System.Globalization.CultureInfo("en-US");
		private static Double ErrorFreeDoubleParse(String p_str)
		{
			if (p_str.Length == 0)
				return 0.0;
			else if (p_str == "Infinity")
				return Double.PositiveInfinity;
			else if (p_str == "-Infinity")
				return Double.NegativeInfinity;
			else if (p_str == "NaN")
				return Double.NaN;

			// completely unnecessary use of unsafe
			unsafe
			{
				Char* p_chars = stackalloc Char[p_str.Length];
				Int32 len = 0;
				Boolean has_sign = false;
				Boolean has_dot = false;
				Boolean has_digit = false;
				for (Int32 i = 0; i < p_str.Length; ++i)
				{
					Char ch = p_str[i];
					switch (ch)
					{
						case '0':
						case '1':
						case '2':
						case '3':
						case '4':
						case '5':
						case '6':
						case '7':
						case '8':
						case '9':
							has_digit = true;
							break;
						case '-':
							if (has_digit || has_sign)
								continue;
							else
								has_sign = true;
							break;
						case '.':
							if (has_dot)
								continue;
							else
								has_dot = true;
							break;
					}

					p_chars[len] = ch;
					++len;
				}

				if (!has_digit)
					return 0.0;

				return Double.Parse(new String(p_chars, 0, len), p_en_culture);
			}
		}
	}
}
