using System;
using UnityEngine;

public enum LogMode
{
    Critical, //Errors only
    Warnings, //Errors and Warnings
    Verbose   //Everthing
}

/// <summary>
/// �����ǵ���־����UnityĬ�ϵ���־,��ϸ��־�Ͳ���ʹ��־Console����̨����������,��������־�ȼ�LogMode���ơ�
/// </summary>
public class LogHandler : ILogHandler
{
    public LogMode mode = LogMode.Critical;

    static LogHandler s_instance;
    ILogHandler m_DefaultLogHandler = Debug.unityLogger.logHandler;//�洢Ĭ��logger���ڴ�ӡ��־

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
        if (logType == LogType.Exception)//�쳣��LogException��������,����Ӧ�����Ǵ�ӡ��.
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
