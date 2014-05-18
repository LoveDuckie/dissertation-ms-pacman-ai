using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsPacmanController
{
	public static class Extensions
	{
		public static List<T> AddMultiple<T>(this List<T> list, params T[] items) {
			foreach( T item in items ) {
				list.Add(item);
			}
			return list;
		}
	}
}
