/*
 * CHƯƠNG TRÌNH TÍNH HÀM MŨ
 * Tác giả  : Nguyễn Khải Hoàn Ca
 * Ngày viết: 7/9/2026
 *
 * Phát biểu đề bài:
 * Nhập vào hai số a và n.
 * Tính giá trị a^n.
 * 
 * Ý tưởng: 
 * Sử dụng hàm Math.Pow(a, n) để tính lũy thừa.
 *
 * Mã giả: 
 * Bắt đầu:
 * 1. Nhập a
 * 2. Nhập n
 * 3. Tính result = a^n
 * 4. In result
 * Kết thúc
 */

using System.IO.Pipelines;

Console.WriteLine("Enter a");
double a = double.Parse(Console.ReadLine());

Console.WriteLine("Enter n");
double n = double.Parse(Console.ReadLine());

double result = Math.Pow(a,n) ; 

Console.WriteLine("a^n = " + result);