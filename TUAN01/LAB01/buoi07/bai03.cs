/*
 * CHƯƠNG TRÌNH TÌM NGÀY TRƯỚC
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập một ngày, tháng, năm và tìm ngày liền trước ngày đó.
 *
 * Ý tưởng: Dùng hàm kiểm tra năm nhuận và hàm xác định số ngày trong tháng để xử lý ngày đầu tháng, đầu năm.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập ngày d, tháng m, năm y
 * 2. Nếu d > 1, ngày trước là d - 1
 * 3. Nếu d = 1, chuyển về ngày cuối tháng trước; riêng tháng 1 chuyển về 31/12 năm trước
 * 4. In ngày trước
 * Kết thúc
 */

using System;

namespace NMLT.Buoi07
{
    class NgayTruoc
    {
        public static bool LaNamNhuan(int y) => (y % 400 == 0) || (y % 4 == 0 && y % 100 != 0);

        public static int SoNgayTrongThang(int m, int y)
        {
            switch (m)
            {
                case 1: case 3: case 5: case 7: case 8: case 10: case 12: return 31;
                case 4: case 6: case 9: case 11: return 30;
                case 2: return LaNamNhuan(y) ? 29 : 28;
                default: return 0;
            }
        }

        public static void TimNgayTruoc(int d, int m, int y, out int d1, out int m1, out int y1)
        {
            if (d > 1)
            {
                d1 = d - 1; m1 = m; y1 = y;
            }
            else if (m == 1)
            {
                d1 = 31; m1 = 12; y1 = y - 1;
            }
            else
            {
                m1 = m - 1;
                d1 = SoNgayTrongThang(m1, y);
                y1 = y;
            }
        }

        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap ngay, thang, nam: ");
            string[] parts = Console.ReadLine().Split();
            int d = int.Parse(parts[0]), m = int.Parse(parts[1]), y = int.Parse(parts[2]);

            TimNgayTruoc(d, m, y, out int d1, out int m1, out int y1);
            Console.WriteLine("Ngay truoc ngay {0}/{1}/{2} la ngay {3}/{4}/{5}.", d, m, y, d1, m1, y1);

            Console.Read();
        }
    }
}
