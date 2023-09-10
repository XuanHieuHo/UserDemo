using UserDemo.Data;
using UserDemo.Models;

namespace UserDemo.Services
{
    public class NguoiDungRepository : INguoiDungRepository
    {
        private readonly MyDbContext _context;

        public NguoiDungRepository(MyDbContext context)
        {
            _context = context;
        }
        public RegisterResModel Add(RegisterModel dangky)
        {
            var _nguoidung = new NguoiDung
            {
                UserName = dangky.UserName,
                Password = dangky.Password,
                Email = dangky.Email,
                HoTen = dangky.HoTen,
            };
            _context.Add(_nguoidung);
            _context.SaveChanges();

            return new RegisterResModel
            {
                Id = _nguoidung.Id,
                UserName = _nguoidung.UserName,
                Password = _nguoidung.Password,
                Email = _nguoidung.Email,
                HoTen = _nguoidung.HoTen,
            };


        }
    }
}
