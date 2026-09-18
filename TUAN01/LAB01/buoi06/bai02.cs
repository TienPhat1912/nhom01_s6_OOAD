/*
 * CHƯƠNG TRÌNH XỬ LÝ CHỮ SỐ
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập số nguyên n và vị trí k, tìm chữ số ở vị trí k, tính tổng các chữ số lẻ, chữ số lẻ nhỏ nhất và lớn nhất.
 *
 * Ý tưởng: Chuyển trị tuyệt đối của n thành chuỗi để lấy chữ số theo vị trí, sau đó dùng phép chia lấy dư để duyệt các chữ số.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n và k
 * 2. Lấy chữ số vị trí k từ trái sang phải
 * 3. Tách từng chữ số của n, kiểm tra chữ số lẻ
 * 4. Cộng và cập nhật chữ số lẻ nhỏ nhất, lớn nhất
 * 5. In kết quả
 * Kết thúc
 */

using System;

namespace NMLT.Buoi06
{
    class XuLyChuSo1
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            int n = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap vi tri k: ");
            int k = int.Parse(Console.ReadLine());

            string s = Math.Abs(n).ToString();
            char csK = (k >= 1 && k <= s.Length) ? s[k - 1] : ' ';

            int tongLe = 0;
            int minLe = 10, maxLe = -1;
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

            Console.WriteLine("Chu so o vi tri {0} (tinh tu trai) la: {1}", k, csK);
            Console.WriteLine("Tong cac chu so le: {0}", tongLe);
            if (maxLe != -1)
                Console.WriteLine("Chu so le nho nhat la {0} va lon nhat la {1}.", minLe, maxLe);
            else
                Console.WriteLine("Khong co chu so le.");

            Console.Read();
        }
    }
}
