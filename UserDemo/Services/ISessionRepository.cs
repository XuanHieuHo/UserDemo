using UserDemo.Data;
using UserDemo.Models;

namespace UserDemo.Services
{
    public interface ISessionRepository
    {
        Session AddSession(SessionReq createSession);
    }
}
