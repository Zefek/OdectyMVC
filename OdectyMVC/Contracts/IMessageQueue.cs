﻿using OdectyMVC.Business;

namespace OdectyMVC.Contracts
{
    public interface IMessageQueue
    {
        Task Publish(int gaugeId, decimal value, DateTime datetime);
    }
}
