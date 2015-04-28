using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorFreeRunTime
{
	/// <summary>
	/// Represents a really odd heap. When running out of memory, it will drop an element farthest from the index where the
	/// new element should have been inserted. When accessing memory locations that were not previously written to, default values are returned.
	/// You also really, really should never be using this anywhere. Just don't.
	/// </summary>
	public class CErrorFreeHeap<T>
	{
		private Dictionary<Double, T> _m_p_elements;

		private List<Double> _m_p_addresses;
		private Boolean _m_out_of_memory;

		/// <summary>
		/// Constructs a new, empty error free heap.
		/// </summary>
		public CErrorFreeHeap()
		{
			_m_p_elements = new Dictionary<Double, T>();
			_m_out_of_memory = false;

			_m_p_addresses = new List<Double>();
		}

		/// <summary>
		/// Writes the given element to the given address of this error free heap.
		/// </summary>
		/// <param name="address">The index to write the element to.</param>
		/// <param name="element">The element to write.</param>
		public void WriteElement(Double address, T element)
		{
			if (Double.IsNaN(address) || Double.IsInfinity(address))
				address = 0.0;

			address = Math.Floor(address);
			if (!_m_out_of_memory)
			{
				try
				{
					_m_p_elements[address] = element;
					Int32 address_index = _m_p_addresses.BinarySearch(address);
					if (address_index < 0)
						address_index = ~address_index;
					_m_p_addresses.Insert(address_index, address);
				}
				catch (OutOfMemoryException)
				{
					_m_out_of_memory = true;

					if (_m_p_addresses.Count != _m_p_elements.Count)
						_m_p_elements.Remove(address);
				}
			}

			if (_m_out_of_memory)
			{
				// we may branch here, even if _m_p_elements.Count is 0, in this case, I will interpret the specification as saying
				// that I am allowed to immediately "drop" the "inserted" value, and thus just bail out.
				if (_m_p_elements.Count == 0)
					return;

				Double dist_to_min = address - _m_p_addresses[0];
				Double dist_to_max = _m_p_addresses[_m_p_addresses.Count - 1] - address;

				if (dist_to_min > dist_to_max)
				{
					// further away from minimum, drop minimum
					_m_p_elements.Remove(_m_p_addresses[0]);

					_m_p_addresses.RemoveAt(0);
					Int32 address_index = _m_p_addresses.BinarySearch(address);
					if (address_index < 0)
						address_index = ~address_index;
					_m_p_addresses.Insert(address_index, address);

					_m_p_elements[address] = element;
				}
				else // further away from maxmimum, or as far away, drop maximum
				{
					_m_p_elements.Remove(_m_p_addresses[_m_p_addresses.Count - 1]);

					_m_p_addresses.RemoveAt(_m_p_addresses.Count - 1);
					Int32 address_index = _m_p_addresses.BinarySearch(address);
					if (address_index < 0)
						address_index = ~address_index;
					_m_p_addresses.Insert(address_index, address);

					_m_p_elements[address] = element;
				}
			}
		}
		/// <summary>
		/// Reads the element at the given address. Returns the default value for the type of the elements if the address has not been written to (or dropped).
		/// </summary>
		/// <param name="address">The address to read the element from.</param>
		/// <returns>Returns the value at the given address, or the default value for the type of the elements if the address has not been written to (or dropped).</returns>
		public T ReadElement(Double address)
		{
			address = Math.Floor(address);
			T element;
			if (!_m_p_elements.TryGetValue(address, out element))
				return default(T);
			else
				return element;
		}
	}
}
