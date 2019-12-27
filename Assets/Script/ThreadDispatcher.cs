using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;


public class ThreadDispatcher
{
    public void RunOnMainThread(Action action)
    {
        lock (_backlog)
        {
            _backlog.Add(action);
            _queued = true;
        }
    }

    public static ThreadDispatcher Initialize()
    {
        if (_instance == null)
        {
            Debug.Log("Init ThreadDispatcher Manager!");
            _instance = new ThreadDispatcher();
        }
        return _instance;
    }

    public void Update()
    {
        if (_queued)
        {
            lock (_backlog)
            {
                var tmp = _actions;
                _actions = _backlog;
                _backlog = tmp;
                _queued = false;
            }

            foreach (var action in _actions)
                action();

            _actions.Clear();
        }
    }

    static ThreadDispatcher _instance;
    static volatile bool _queued = false;
    static List<Action> _backlog = new List<Action>(8);
    static List<Action> _actions = new List<Action>(8);
}