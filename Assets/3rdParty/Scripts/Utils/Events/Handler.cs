using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Events
{

	public abstract class HandlerBase
	{
		public static bool LogsEnabled
		{
			get
			{
				return false;
			}
		}

		public static bool AllFireLogs
		{
			get
			{
				return LogsEnabled;
			}
		}

		public List<object> Watchers
		{
			get
			{
				return _watchers;
			}
		}

		protected List<object> _watchers = new List<object>(100);

		public virtual void CleanUp()
		{
	
		}

		public virtual bool FixWatchers()
		{
			return false;
		}

		protected void EnsureWatchers()
		{
			if (_watchers == null)
			{
				_watchers = new List<object>(100);
			}
		}
	}

	public class Handler<T> : HandlerBase
	{

		List<Action<T>> _actions = new List<Action<T>>(100);
		List<Action<T>> _removed = new List<Action<T>>(100);

		public void Subscribe(object watcher, Action<T> action)
		{
			if (_removed.Contains(action))
			{
				_removed.Remove(action);
			}
			if (!_actions.Contains(action))
			{
				_actions.Add(action);
				EnsureWatchers();
				_watchers.Add(watcher);
			}
			else if (LogsEnabled)
			{
				Debug.LogError($"{ watcher} tries to subscribe to { action} again.");
			}
		}

		public void Unsubscribe(Action<T> action)
		{
			SafeUnsubscribe(action);
		}

		void SafeUnsubscribe(Action<T> action)
		{
			var index = _actions.IndexOf(action);
			SafeUnsubscribe(index);
			if (index < 0 && LogsEnabled)
			{
				Debug.LogError($"Trying to unsubscribe action {action} without watcher.");
			}
		}

		void SafeUnsubscribe(int index)
		{
			if (index >= 0)
			{
				_removed.Add(_actions[index]);
			}
		}

		void FullUnsubscribe(int index)
		{
			if (index >= 0)
			{
				_actions.RemoveAt(index);
				if (index < _watchers.Count)
				{
					_watchers.RemoveAt(index);
				}
			}
		}

		void FullUnsubscribe(Action<T> action)
		{
			var index = _actions.IndexOf(action);
			FullUnsubscribe(index);
		}

		public void Fire(T arg)
		{
			for (int i = 0; i < _actions.Count; i++)
			{
				var current = _actions[i];
				if (!_removed.Contains(current))
				{
					current.Invoke(arg);
				}
			}
			CleanUp();
		}

		public override void CleanUp()
		{
			var iter = _removed.GetEnumerator();
			while (iter.MoveNext())
			{
				FullUnsubscribe(iter.Current);
			}
			_removed.Clear();
		}

		public override bool FixWatchers()
		{
			CleanUp();
			int count = 0;
			EnsureWatchers();
			for (int i = 0; i < _watchers.Count; i++)
			{
				var watcher = _watchers[i];
				if (watcher is MonoBehaviour)
				{
					var comp = watcher as MonoBehaviour;
					if (!comp)
					{
						SafeUnsubscribe(i);
						count++;
					}
				}
			}
			if (count > 0)
			{
				CleanUp();
			}
			return count == 0;
		}
	}
}