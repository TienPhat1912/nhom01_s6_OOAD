/*
* CHƯƠNG TRÌNH TÍNH GIÁ TRỊ HÀM SỐ 1
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 9/9/2026
*
* Phát biểu đề bài:
* Nhập vào một số thực x.
* Tính giá trị của hai hàm f1(x) và f2(x)
* theo từng khoảng giá trị của x.
*
* Ý tưởng:
* Xây dựng hai hàm f1(x) và f2(x).
* Sử dụng cấu trúc if - else để kiểm tra giá trị của x
* và chọn công thức phù hợp.
*
* Hàm f1(x):
* - Nếu x <= 0       thì f1(x) = 0
* - Nếu 0 < x <= 1   thì f1(x) = x
* - Nếu x > 1        thì f1(x) = x^4
*
* Hàm f2(x):
* - Nếu x <= 2       thì f2(x) = x^2 + 4x + 5
* - Nếu x > 2        thì f2(x) = 1 / (x^2 + 4x + 5)
*
* Mã giả:
* 1. Nhập x
* 2. Nếu x <= 0 thì f1 = 0
* 3. Ngược lại nếu x <= 1 thì f1 = x
* 4. Ngược lại f1 = x^4
* 5. Nếu x <= 2 thì f2 = x^2 + 4x + 5
* 6. Ngược lại f2 = 1 / (x^2 + 4x + 5)
* 7. In f1(x) và f2(x)
*/

using System;

class funtionCalc
{
    static double f1(double x)
    {
        if(x <= 0)
        {
            return 0;
        } else if (x <= 1)
        {
            return x;
        }     else
        {
            return Math.Pow(x,4);
        }
    }

    static double f2(double x)
    {
        if (x <=2)
        {
            return Math.Pow(x,2) + 4*x + 5;
        } else
        {
            return 1/(Math.Pow(x,2) + 4*x + 5);
        }
    }

    static void Main()
    {
        Console.WriteLine("Enter x: ");
        double x = double.Parse(Console.ReadLine());

        Console.WriteLine($"""
        f1({x}) = {f1(x)}
        f2({x}) = {f2(x)}
        """);
    }
}