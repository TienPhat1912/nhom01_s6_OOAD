/*
 * CHƯƠNG TRÌNH XỬ LÝ CHỮ SỐ BẰNG HÀM
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập số nguyên n và vị trí k, tìm chữ số ở vị trí k, tổng các chữ số lẻ, chữ số lẻ nhỏ nhất và lớn nhất.
 *
 * Ý tưởng: Gom toàn bộ thao tác xử lý vào hàm XuLy, trả kết quả qua các tham số ref.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n và k
 * 2. Gọi hàm XuLy để lấy chữ số vị trí k
 * 3. Trong hàm, duyệt các chữ số, cộng chữ số lẻ và cập nhật min, max
 * 4. In các kết quả
 * Kết thúc
 */

using System;

namespace NMLT.Buoi07
{
    class XuLyChuSo1
    {
        public static void XuLy(int n, int k, ref char csK, ref int tongLe, ref int minLe, ref int maxLe)
        {
            string s = Math.Abs(n).ToString();
            csK = (k >= 1 && k <= s.Length) ? s[k - 1] : ' ';

            tongLe = 0; minLe = 10; maxLe = -1;
            int temp = Math.Abs(n);

            while (temp > 0)
            {
                int cs = temp % 10;
                if (cs % 2 != 0)
                {
                    tongLe += cs;
                    if (cs < minLe) minLe = cs;
                    if (cs > maxLe) maxLe = cs;
                }
                temp /= 10;
            }
        }

        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            int n = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap vi tri k: ");
            int k = int.Parse(Console.ReadLine());

            char csK = ' '; int tongLe = 0, minLe = 0, maxLe = 0;
            XuLy(n, k, ref csK, ref tongLe, ref minLe, ref maxLe);

            Console.WriteLine("Chu so o vi tri {0} (tinh tu trai) la {1}.", k, csK);
            Console.WriteLine("Tong cac chu so le: {0}.", tongLe);
            if (maxLe != -1)
                Console.WriteLine("Chu so le nho nhat la {0} va lon nhat la {1}.", minLe, maxLe);

            Console.Read();
        }
    }
}
