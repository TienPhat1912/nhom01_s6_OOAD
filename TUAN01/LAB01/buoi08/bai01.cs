/*
 * CHƯƠNG TRÌNH TÍNH TỔ HỢP
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập k và n, tính tổ hợp C(k,n) = n! / (k!(n-k)!).
 *
 * Ý tưởng: Dùng hàm TinhGiaiThua để tính các giai thừa, sau đó dùng hàm TinhToHop áp dụng công thức tổ hợp.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập k và n
 * 2. Tính n!, k! và (n-k)!
 * 3. Tính C(k,n) = n! / (k!(n-k)!)
 * 4. In kết quả
 * Kết thúc
 */

using System;

namespace NMLT.Buoi08
{
    class GiaiThua
    {
        public static long TinhGiaiThua(int n)
        {
            long gt = 1;
            for (int i = 1; i <= n; i++) gt *= i;
            return gt;
        }

        public static long TinhToHop(int k, int n)
        {
            return TinhGiaiThua(n) / (TinhGiaiThua(k) * TinhGiaiThua(n - k));
        }

        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap k: ");
            int k = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap n: ");
            int n = int.Parse(Console.ReadLine());

            Console.WriteLine("C({0}, {1}) = {2}.", k, n, TinhToHop(k, n));
            Console.Read();
        }
    }
}
