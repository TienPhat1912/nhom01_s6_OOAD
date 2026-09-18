/*
 * CHƯƠNG TRÌNH TÍNH GIÁ TRỊ BIỂU THỨC
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập số nguyên n, tính hai tổng S1 và S2 theo các phân số từ 1 đến n.
 *
 * Ý tưởng: Dùng vòng lặp cộng các số hạng của tổng thuận và tổng đan dấu.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n
 * 2. Tính S1 = 1 + 1/2 + ... + 1/n
 * 3. Tính S2 = 1/n - 1/(n-1) + ... với dấu luân phiên
 * 4. In S1 và S2
 * Kết thúc
 */

using System;

namespace NMLT.Buoi05
{
    class GiaTriBieuThuc
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so nguyen n: ");
            int n = int.Parse(Console.ReadLine());

            double s1 = 0;
            for (int i = 1; i <= n; i++)
            {
                s1 += 1.0 / i;
            }

            double s2 = 0;
            int dau = 1;
            for (int i = n; i >= 1; i--)
            {
                s2 += dau * (1.0 / i);
                dau = -dau;
            }

            Console.WriteLine("S1({0}) = {1:0.00}", n, s1);
            Console.WriteLine("S2({0}) = {1:0.00}", n, s2);
            Console.Read();
        }
    }
}
