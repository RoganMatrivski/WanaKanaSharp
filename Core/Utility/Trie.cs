//
// RadixTree.cs
//
// Author:
//       John Mark Gabriel Caguicla <caguicla.jmg@hapticbunnystudios.com>
//
// Copyright (c) 2018 John Mark Gabriel Caguicla
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WanaKanaSharp.Utility
{
	public class Trie<TKey, TValue>
	{
		public class Node : IEnumerable<Node>
		{
			class Enumerator : IEnumerator<Node>
			{
				Dictionary<TKey, Node>.Enumerator DictionaryEnumerator;

				public Node Current => DictionaryEnumerator.Current.Value;

				object IEnumerator.Current => Current;

				public Enumerator(Dictionary<TKey, Node>.Enumerator enumerator)
				{
					DictionaryEnumerator = enumerator;
				}

				public void Dispose()
				{
					DictionaryEnumerator.Dispose();
				}

				public bool MoveNext() => DictionaryEnumerator.MoveNext();

				public void Reset() => throw new NotImplementedException();
			}

			public TKey Key { get; private set; }
			public TValue Value { get; set; }

			Dictionary<TKey, Node> Children = new Dictionary<TKey, Node>();
			List<Node> Parents { get; } = new List<Node>();

			public Node(TKey key, TValue value)
			{
				Key = key;
				Value = value;
			}

			public Node this[TKey key] => Children[key];

			public Node Attach(Node node)
			{
				if (ReferenceEquals(this, node)) throw new ArgumentException(""); // TODO: 
				if (node.IsDescendant(this)) throw new ArgumentException(""); // TODO:

				node.Parents.Add(this);
				Children.Add(node.Key, node);
				return node;
			}

			public void Attach(params Node[] nodes)
			{
				foreach (var node in nodes)
				{
					Attach(node);
				}
			}

			public Boolean ContainsKey(TKey key) => Children.ContainsKey(key);

			public void Detach(params Node[] nodes)
			{
				foreach (var node in nodes)
				{
					if (!IsChild(node)) continue;

					foreach (var child in this)
					{
						if (ReferenceEquals(child, node))
						{
							node.Parents.Remove(this);
							Children.Remove(child.Key);
						}
					}
				}
			}

			public Node Duplicate(Boolean copyChildren = false)
			{
				var node = new Node(Key, Value);

				if (copyChildren)
				{
					foreach (var child in this)
					{
						node.Attach(child.Duplicate(true));
					}
				}

				return node;
			}

			public Node GetChild(TKey key)
			{
				if (Children.TryGetValue(key, out Node node)) return node;

				return null;
			}

			public IEnumerator<Node> GetEnumerator()
			{
				return new Enumerator(Children.GetEnumerator());
			}

			public Node Insert((TKey Key, TValue Value) pair)
			{
				var node = new Node(pair.Key, pair.Value);
				return Attach(node);
			}

			public void Insert(params (TKey Key, TValue Value)[] pairs)
			{
				foreach (var pair in pairs)
				{
					Insert(pair);
				}
			}

			public Boolean IsAncestor(Node node)
			{
				if (IsParent(node)) return true;

				foreach (var parent in Parents) if (parent.IsAncestor(node)) return true;

				return false;
			}

			public Boolean IsChild(Node node)
			{
				return Children.Count((child) => ReferenceEquals(child.Value, node)) > 0;
			}

			public Boolean IsDescendant(Node node)
			{
				if (IsChild(node)) return true;

				foreach (var child in this) if (child.IsDescendant(node)) return true;

				return false;
			}

			public Boolean IsParent(Node node)
			{
				return Parents.Count((parent) => ReferenceEquals(parent, node)) > 0;
			}

			public Boolean IsRoot() => Parents.Count == 0;

			public Boolean IsSibling(Node node)
			{
				foreach (var parent in Parents) if (parent.IsChild(node)) return true;

				return false;
			}

			public void Remove(params TKey[] keys)
			{
				foreach (var key in keys)
				{
					if (!Children.ContainsKey(key)) continue;

					Detach(Children[key]);
				}
			}

			public void Traverse(Action<Node> action, Int32 maxDepth = -1)
			{
				Traverse(action, 0, maxDepth);
			}

			void Traverse(Action<Node> action, Int32 currentDepth, Int32 maxDepth)
			{
				action(this);

				if (currentDepth == maxDepth) return;

				foreach (var child in this)
				{
					child.Traverse(action, currentDepth + 1, maxDepth);
				}
			}

			public void TraverseChildren(Action<Node> action, Int32 maxDepth = 0)
			{
				foreach (var child in this)
				{
					child.Traverse(action, 0, maxDepth);
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public Node this[TKey key]
		{
			get { return Root[key]; }
		}

		public Node Root { get; } = new Node(default(TKey), default(TValue));

		public void Merge(Trie<TKey, TValue> trie, Boolean duplicate = false)
		{
			var root = trie.Root;
			var children = root.ToArray();

			foreach (var node in children)
			{
				Root.Attach(duplicate ? node.Duplicate(true) : node);
				if (!duplicate) root.Detach(node);
			}
		}

		public static Trie<TKey, TValue> Merge(Trie<TKey, TValue> a, Trie<TKey, TValue> b, Boolean duplicate = false)
		{
			var trie = new Trie<TKey, TValue>();

			trie.Merge(a, duplicate);
			trie.Merge(b, duplicate);

			return trie;
		}
	}
}
