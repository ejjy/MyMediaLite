// Copyright (C) 2010, 2011 Zeno Gantner
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MyMediaLite.Data;
using MyMediaLite.DataType;

/*! \namespace MyMediaLite.Util
 *  \brief This namespace contains helper code that did not fit anywhere else.
 */
namespace MyMediaLite.Util
{
	/// <summary>Class containing utility functions</summary>
	public static class Utils
	{
		// TODO add memory constraints and a replacement strategy
		/// <summary>Memoize a function</summary>
		/// <param name="f">The function to memoize</param>
		/// <returns>a version of the function that remembers past function results</returns>
		public static Func<A, R> Memoize<A, R>(this Func<A, R> f)
		{
			var map = new Dictionary<A, R>();
			return a =>
			{
				R value;
				if (map.TryGetValue(a, out value))
				return value;
				value = f(a);
				map.Add(a, value);
				return value;
			};
		}

		/// <summary>Shuffle a list in-place</summary>
		/// <remarks>
		/// Fisher-Yates shuffle, see
		/// http://en.wikipedia.org/wiki/Fisher–Yates_shuffle
		/// </remarks>
		public static void Shuffle<T>(IList<T> list)
		{
			Random random = MyMediaLite.Util.Random.GetInstance();
			for (int i = list.Count - 1; i >= 0; i--)
			{
				int r = random.Next(i + 1);

				// swap position i with position r
				T tmp = list[i];
				list[i] = list[r];
				list[r] = tmp;
			}
		}

		/// <summary>Get all types of a namespace</summary>
		/// <param name="name_space">a string describing the namespace</param>
		/// <returns>an array of Type objects</returns>
		public static Type[] GetTypesInNamespace(string name_space)
		{
			var types = new List<Type>();

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
				types.AddRange( assembly.GetTypes().Where(t => string.Equals(t.Namespace, name_space, StringComparison.Ordinal)) );

			return types.ToArray();
		}

		/// <summary>Display dataset statistics</summary>
		/// <param name="train">the training data</param>
		/// <param name="test">the test data</param>
		/// <param name="user_attributes">the user attributes</param>
		/// <param name="item_attributes">the item attributes</param>
		/// <param name="display_overlap">if set true, display the user/item overlap between train and test</param>
		public static void DisplayDataStats(
			IRatings train, IRatings test,
			SparseBooleanMatrix user_attributes, SparseBooleanMatrix item_attributes,
			bool display_overlap = false)
		{
			// training data stats
			int num_users = train.AllUsers.Count;
			int num_items = train.AllItems.Count;
			long matrix_size = (long) num_users * num_items;
			long empty_size  = (long) matrix_size - train.Count;
			double sparsity = (double) 100L * empty_size / matrix_size;
			Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "training data: {0} users, {1} items, {2} ratings, sparsity {3,0:0.#####}", num_users, num_items, train.Count, sparsity));
			if (train is ITimedRatings)
			{
				var time_train = train as ITimedRatings;
				Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "rating period: {0} to {1}", time_train.EarliestTime, time_train.LatestTime));
			}

			// test data stats
			if (test != null)
			{
				num_users = test.AllUsers.Count;
				num_items = test.AllItems.Count;
				matrix_size = (long) num_users * num_items;
				empty_size  = (long) matrix_size - test.Count; // TODO depends on the eval scheme whether this is correct
				sparsity = (double) 100L * empty_size / matrix_size;
				Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "test data:     {0} users, {1} items, {2} ratings, sparsity {3,0:0.#####}", num_users, num_items, test.Count, sparsity));
				if (train is ITimedRatings)
				{
					var time_train = train as ITimedRatings;
					Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "rating period: {0} to {1}", time_train.EarliestTime, time_train.LatestTime));
				}
			}

			// count and display the overlap between train and test
			if (display_overlap && test != null)
			{
				int num_new_users = 0;
				int num_new_items = 0;
				TimeSpan seconds = Wrap.MeasureTime(delegate() {
							num_new_users = test.AllUsers.Except(train.AllUsers).Count();
							num_new_items = test.AllItems.Except(train.AllItems).Count();
				});
				Console.WriteLine("{0} new users, {1} new items ({2} seconds)", num_new_users, num_new_items, seconds);
			}

			DisplayAttributeStats(user_attributes, item_attributes);
		}

		/// <summary>Display data statistics for item recommendation datasets</summary>
		/// <param name="training_data">the training dataset</param>
		/// <param name="test_data">the test dataset</param>
		/// <param name="user_attributes">the user attributes</param>
		/// <param name="item_attributes">the item attributes</param>
		public static void DisplayDataStats(
			IPosOnlyFeedback training_data, IPosOnlyFeedback test_data,
			SparseBooleanMatrix user_attributes, SparseBooleanMatrix item_attributes)
		{
			// training data stats
			int num_users = training_data.AllUsers.Count;
			int num_items = training_data.AllItems.Count;
			long matrix_size = (long) num_users * num_items;
			long empty_size  = (long) matrix_size - training_data.Count;
			double sparsity = (double) 100L * empty_size / matrix_size;
			Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "training data: {0} users, {1} items, {2} events, sparsity {3,0:0.#####}", num_users, num_items, training_data.Count, sparsity));

			// test data stats
			if (test_data != null)
			{
				num_users = test_data.AllUsers.Count;
				num_items = test_data.AllItems.Count;
				matrix_size = (long) num_users * num_items;
				empty_size  = (long) matrix_size - test_data.Count;
				sparsity = (double) 100L * empty_size / matrix_size; // TODO depends on the eval scheme whether this is correct
				Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "test data:     {0} users, {1} items, {2} events, sparsity {3,0:0.#####}", num_users, num_items, test_data.Count, sparsity));
			}

			DisplayAttributeStats(user_attributes, item_attributes);
		}

		/// <summary>Display statistics for user and item attributes</summary>
		/// <param name="user_attributes">the user attributes</param>
		/// <param name="item_attributes">the item attributes</param>
		public static void DisplayAttributeStats(SparseBooleanMatrix user_attributes, SparseBooleanMatrix item_attributes)
		{
			if (user_attributes != null)
			{
				Console.WriteLine(
					"{0} user attributes for {1} users, {2} assignments, {3} users with attribute assignments",
					user_attributes.NumberOfColumns, user_attributes.NumberOfRows,
					user_attributes.NumberOfEntries, user_attributes.NonEmptyRowIDs.Count);
			}
			if (item_attributes != null)
				Console.WriteLine(
					"{0} item attributes for {1} items, {2} assignments, {3} items with attribute assignments",
					item_attributes.NonEmptyColumnIDs.Count, item_attributes.NumberOfRows,
					item_attributes.NumberOfEntries, item_attributes.NonEmptyRowIDs.Count);
		}
	}
}
