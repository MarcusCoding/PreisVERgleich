using System;

namespace PreisVergleich.Helpers
{
    public class Logger
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public void writeLog(LogType type, string message, Exception ex = null)
        {
            switch (type)
            {
                case LogType.INFO:
                    logger.Info(message);
                    break;
                case LogType.DEBUG:
                    logger.Debug(message);
                    break;
                case LogType.WARNING:
                    logger.Warn(message);
                    break;
                case LogType.ERROR:
                    if(ex == null)
                    {
                        logger.Error(message);
                    }
                    else
                    {
                        logger.Error(ex, message);
                    }
                    break;
            }        
        }

        public enum LogType
        {
            INFO = 0,
            DEBUG = 1,
            WARNING = 2,
            ERROR = 3
        }
    }
}
