/*
 * CHƯƠNG TRÌNH TÍNH LŨY THỪA NHANH 1
 * Tác giả  : Nguyễn Khải Hoàn Ca
 * Ngày viết: 8/9/2026
 * 
 * Phát biểu đề bài:
 * Nhập vào một số thực a.
 * Tính và in ra các giá trị a^2, a^5 và a^17.
 *
 * Ý tưởng:
 * Nhập số thực a từ bàn phím.
 * Sử dụng hàm Math.Pow(a, n) để tính lần lượt
 * a^2, a^5 và a^17.
 *
 * Mã giả:
 * 1. Nhập a
 * 2. Tính a^2
 * 3. Tính a^5
 * 4. Tính a^17
 * 5. In các kết quả
 */

Console.WriteLine("Enter a real number : ");
double a = double.Parse(Console.ReadLine());

Console.WriteLine(
    $"{a}^2 = {Math.Pow(a, 2)} | " +
    $"{a}^5 = {Math.Pow(a, 5)} | " +
    $"{a}^17 = {Math.Pow(a, 17)}"
);