/*
 * CHƯƠNG TRÌNH THỐNG KÊ CHỮ SỐ
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập số nguyên n, đếm và in số lần xuất hiện của từng chữ số trong n.
 *
 * Ý tưởng: Với mỗi chữ số từ 0 đến 9, dùng hàm DemXuatHien để duyệt n bằng phép chia lấy dư.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n
 * 2. Với digit từ 0 đến 9, đếm số lần digit xuất hiện trong n
 * 3. Nếu số lần xuất hiện lớn hơn 0, in kết quả
 * Kết thúc
 */

using System;

namespace NMLT.Buoi08
{
    class DemChuSo
    {
        public static int DemXuatHien(long n, int digit)
        {
            int dem = 0;
            long temp = Math.Abs(n);
            while (temp > 0)
            {
                if (temp % 10 == digit) dem++;
                temp /= 10;
            }
            return dem;
        }

        public static void ThongKeChuSo(long n)
        {
            for (int d = 0; d <= 9; d++)
            {
                int dem = DemXuatHien(n, d);
                if (dem > 0)
                {
                    Console.WriteLine("Chu so {0} xuat hien {1} lan.", d, dem);
                }
            }
        }

        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            long n = long.Parse(Console.ReadLine());

            ThongKeChuSo(n);
            Console.Read();
        }
    }
}
