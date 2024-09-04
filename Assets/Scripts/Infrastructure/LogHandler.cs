using System;
using UnityEngine;

public enum LogMode
{
    Critical, //Errors only
    Warnings, //Errors and Warnings
    Verbose   //Everthing
}

/// <summary>
/// 用我们的日志覆盖Unity默认的日志,详细日志就不会使日志Console控制台看起来混乱,可以用日志等级LogMode控制。
/// </summary>
public class LogHandler : ILogHandler
{
    public LogMode mode = LogMode.Critical;

    static LogHandler s_instance;
    ILogHandler m_DefaultLogHandler = Debug.unityLogger.logHandler;//存储默认logger用于打印日志

    public static LogHandler Get()
    {
        if (s_instance != null) return s_instance;
        s_instance = new LogHandler();
        Debug.unityLogger.logHandler = s_instance;
        return s_instance;
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        m_DefaultLogHandler.LogException(exception, context);
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        if (logType == LogType.Exception)//异常被LogException函数捕获,并且应该总是打印的.
            return;

        if (logType == LogType.Error || logType == LogType.Assert)
        {
            m_DefaultLogHandler.LogFormat(logType, context, format, args);
            return;
        }

        if (mode == LogMode.Critical)
            return;

        if(logType == LogType.Warning)
        {
            m_DefaultLogHandler.LogFormat(logType, context, format, args);
            return;
        }

        if (mode == LogMode.Warnings)
            return;

        m_DefaultLogHandler.LogFormat(logType, context, format, args);
    }
}
