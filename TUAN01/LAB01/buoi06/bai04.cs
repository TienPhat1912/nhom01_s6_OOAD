/*
 * CHƯƠNG TRÌNH KIỂM TRA SỐ HOÀN HẢO
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập n, kiểm tra n có phải là số hoàn hảo hay không.
 *
 * Ý tưởng: Tính tổng các ước dương thực sự của n, rồi so sánh tổng đó với n.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n
 * 2. Duyệt các số từ 1 đến n/2, nếu là ước thì cộng vào tổng
 * 3. Nếu tổng bằng n và n > 0, kết luận n là số hoàn hảo
 * 4. In kết luận
 * Kết thúc
 */

using System;

namespace NMLT.Buoi06
{
    class KiemTraSoHoanHao
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap n: ");
            int n = int.Parse(Console.ReadLine());

            int tongUoc = 0;
            for (int i = 1; i <= n / 2; i++)
            {
                if (n % i == 0)
                    tongUoc += i;
            }

            if (tongUoc == n && n > 0)
                Console.WriteLine("{0} la so hoan hao.", n);
            else
                Console.WriteLine("{0} khong la so hoan hao.", n);

            Console.Read();
        }
    }
}
