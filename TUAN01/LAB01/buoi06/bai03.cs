/*
 * CHƯƠNG TRÌNH PHÂN TÍCH THỪA SỐ NGUYÊN TỐ
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập số nguyên n > 1, phân tích n thành tích các thừa số nguyên tố.
 *
 * Ý tưởng: Thử lần lượt các ước từ 2, chia liên tiếp khi tìm được ước và đếm số lần xuất hiện.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n
 * 2. Với i từ 2, nếu i là ước của n thì chia n và đếm số mũ
 * 3. In các thừa số nguyên tố và số mũ
 * 4. Nếu phần còn lại lớn hơn 1, in phần đó
 * Kết thúc
 */

using System;

namespace NMLT.Buoi06
{
    class PhanTichThuaSo1
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so nguyen n (n > 1): ");
            int n = int.Parse(Console.ReadLine());

            Console.Write("{0} = ", n);
            int temp = n;
            bool dauTien = true;

            for (int i = 2; i * i <= temp; i++)
            {
                if (temp % i == 0)
                {
                    int dem = 0;
                    while (temp % i == 0)
                    {
                        dem++;
                        temp /= i;
                    }
                    if (!dauTien) Console.Write(" x ");
                    Console.Write(dem > 1 ? "{0}^{1}" : "{0}", i, dem);
                    dauTien = false;
                }
            }
            if (temp > 1)
            {
                if (!dauTien) Console.Write(" x ");
                Console.Write("{0}", temp);
            }
            Console.WriteLine();
            Console.Read();
        }
    }
}
