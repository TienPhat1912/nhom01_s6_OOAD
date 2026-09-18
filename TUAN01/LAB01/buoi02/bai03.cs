/*
* CHƯƠNG TRÌNH TÍNH BIỂU THỨC NHANH 1
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 8/9/2026
*
* Phát biểu đề bài:
* Nhập vào một số thực x.
* Tính giá trị của hàm:
* f(x) = 1 + 2x + 3x^2 - 4x^3
*
* Ý tưởng:
* Nhập x từ bàn phím.
* Sử dụng Math.Pow() để tính x^2 và x^3.
* Thay x vào biểu thức để tính f(x).
*
* Mã giả:
* 1. Nhập x
* 2. Tính fx = 1 + 2*x + 3*x^2 - 4*x^3
* 3. In fx
*/

Console.WriteLine("Enter x: ");
double x = double.Parse(Console.ReadLine());

double fx = 1 + 2 * x + 3 * Math.Pow(x,2) - 4 * Math.Pow(x,3);

Console.WriteLine("f(x) = 1 + 2x + 3x^2 - 4x^3");
Console.WriteLine("f(" + x + ") =" + fx);