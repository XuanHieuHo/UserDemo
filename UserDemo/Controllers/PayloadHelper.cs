using UserDemo.Models;

namespace UserDemo.Controllers
{
    public class PayloadHelper
    {
        public Payload NewPayload(int username, TimeSpan duration)
        {
            var tokenID = Guid.NewGuid();
            var issuedAt = DateTime.Now;
            var expiredAt = issuedAt.Add(duration);

            var payload = new Payload
            {
                Id = tokenID,
                UserNameId = username.ToString(),
                IssuedAt = issuedAt,
                ExpiredAt = expiredAt
            };


            return payload;
        }
    }
}
