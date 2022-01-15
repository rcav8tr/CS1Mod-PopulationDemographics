using System;
using System.Reflection;
using UnityEngine;

namespace PopulationDemographics
{
    /// <summary>
    /// utility routines for logging
    /// </summary>
    public class LogUtil
    {
        // text to place in front of messages to make them easier to find in the log file
        // assumes the namespace of LogUtil is same namespace used thoughout the project
        private static readonly string MessagePrefix = "[" + typeof(LogUtil).Namespace + "] ";

        /// <summary>
        /// log an info message
        /// </summary>
        public static void LogInfo(string message)
        {
            Debug.Log(MessagePrefix + message);
        }

        /// <summary>
        /// log a warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning(MessagePrefix + message);
        }

        /// <summary>
        /// log an error message with the calling method
        /// </summary>
        public static void LogError(string message)
        {
            // construct the message
            System.Diagnostics.StackTrace stacktrace = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame[] stackFrames = stacktrace.GetFrames();
            if (stackFrames.Length >= 2)
            {
                // prefix message with calling method instead of the standard message prefix
                MethodBase method = stackFrames[1].GetMethod();
                message = "[" + method.ReflectedType + "." + method.Name + "] Error:" + Environment.NewLine + message;
            }
            else
            {
                // just use the prefix alone
                message = MessagePrefix + "Error: " + message;
            }

            // log the message as an error
            Debug.LogError(message);
        }

        /// <summary>
        /// log an exception
        /// </summary>
        public static void LogException(Exception ex)
        {
            // the default exception string includes the message and the stack trace
            // the stack trace includes the namespace, so no need to include the standard message prefix
            Debug.LogError(ex.ToString());
        }

        /// <summary>
        /// log a stack trace as an error
        /// </summary>
        public static void LogStackTrace()
        {
            // the first entry will be for this LogStackTrace routine
            // the stack trace includes the namespace, so no need to include the standard message prefix
            Debug.LogError(new System.Diagnostics.StackTrace().ToString());
        }
    }
}
