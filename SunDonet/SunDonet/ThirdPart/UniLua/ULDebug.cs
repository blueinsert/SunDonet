﻿

using System;

namespace UniLua.Tools
{
#if UNITY
	// using Logger = DebugAssist;
	using Logger = UnityEngine.Debug;
#endif

    // thanks to dharco
    // refer to https://github.com/dharco/UniLua/commit/2854ddf2500ab2f943f01a6d3c9af767c092ce75
    public class ULDebug
	{
		public static System.Action<object> Log = NoAction;
		public static System.Action<object> LogError = NoAction;

		private static void NoAction(object msg) { }

		static ULDebug()
		{
#if UNITY
			Log = Logger.Log;
			LogError = Logger.LogError;
#else
            Log = Console.WriteLine;
            LogError = Console.WriteLine;
#endif
        }
	}
}

