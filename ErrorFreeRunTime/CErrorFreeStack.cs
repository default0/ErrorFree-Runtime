using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorFreeRunTime
{
	/// <summary>
	/// Represents a really odd stack that, rather than running out of memory, will drop things. It will also return default values when its empty and you pop it, so beware.
	/// It really isn't something you should be using, ever.
	/// </summary>
	internal class CErrorFreeStack<T>
	{
		// There does not appear to be a sane way of dealing with numeric overflows incase the framework ever allows a list to grow that large before
		// it actually throws an OutOfMemoryException. The indexer of List<T> does not allow access with longs anyways, and I'm too lazy to actually
		// have a pseudo-list-implementation here just so I can use Int64 for indices. Arrays are not allowed to grow beyond 2 147 483 591 billion elements
		// in the .NET Framework anyways, and I doubt that this is going to change anytime soon, therefore I'm not going to bother with actually
		// trying to make sure that I can support such stupidly large stacks and will just drop elements as soon as I get an OutOfMemoryException.

		private List<T> _m_p_elements;

		private Int32 _m_read_position;
		private Int32 _m_position;
		private Int32 _m_available_elements;

		private Boolean _m_no_more_memory;

		/// <summary>
		/// Constructs a new, empty dropping stack.
		/// </summary>
		public CErrorFreeStack()
		{
			_m_p_elements = new List<T>();
			_m_read_position = 0;
			_m_position = 0;
			_m_available_elements = 0;
			_m_no_more_memory = false;
		}

		/// <summary>
		/// Pushes a new element on the dropping stack. If the stack is full and cannot be extended further, elements will be dropped.
		/// </summary>
		/// <param name="element">The element to push.</param>
		public void Push(T element)
		{
			if (!_m_no_more_memory)
			{
				try
				{
					if (_m_position < _m_p_elements.Count)
					{
						_m_p_elements[_m_position] = element;
					}
					else
					{
						_m_p_elements.Add(element);
					}
					++_m_available_elements;
					++_m_position;
				}
				catch (OutOfMemoryException)
				{
					_m_no_more_memory = true;

					if (_m_position >= _m_p_elements.Count)
						_m_position = 0;
				}
			}
			if (_m_no_more_memory)
			{
				// we may branch here, even if _m_p_elements.Count is 0, in this case, I will interpret the specification as saying
				// that I am allowed to immediately "drop" the "inserted" value, and thus just bail out.
				if (_m_p_elements.Count == 0)
					return;

				_m_p_elements[_m_position] = element;

				++_m_available_elements;
				++_m_position;
				if (_m_position >= _m_p_elements.Count)
					_m_position = 0;
			}
		}
		/// <summary>
		/// Peeks at the top-most element from the stack. If the stack is empty, the default value for the type will be returned instead.
		/// </summary>
		/// <returns>Peeks at the top-most element from the stack. If the stack is empty, the default value for the type will be returned instead.</returns>
		public T Peek()
		{
			Int32 position = _m_position - 1;
			if (position < 0)
				position = _m_p_elements.Count - 1;

			return _m_p_elements[position];
		}
		/// <summary>
		/// Pops the top-most element from the dropping stack. If the stack is empty the default value for the type will be returned instead.
		/// </summary>
		/// <returns>Returns the top-most element from the dropping stack, or the default value for the elements if the stack is empty.</returns>
		public T Pop()
		{
			if (_m_available_elements == 0)
				return default(T);

			--_m_available_elements;
			--_m_position;
			if (_m_position < 0)
				_m_position = _m_p_elements.Count - 1;

			return _m_p_elements[_m_position];
		}
	}
}
