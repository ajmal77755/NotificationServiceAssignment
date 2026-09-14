using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Domain
{
    public enum ForwardingStatus
    {
        NotRequired = 0,
        Pending = 1,
        Sent = 2,
        Failed = 3
    }
}
