/*
* CHƯƠNG TRÌNH GIẢI PHƯƠNG TRÌNH BẬC 2
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 9/9/2026
*
* Phát biểu đề bài:
* Nhập vào ba hệ số a, b, c của phương trình:
* ax^2 + bx + c = 0
* Giải và in ra nghiệm của phương trình.
*
* Ý tưởng:
* Kiểm tra a:
* - Nếu a = 0 thì không phải phương trình bậc 2.
* - Nếu a != 0 thì tính:
*   delta = b^2 - 4ac
*
* Xét delta:
* - delta < 0  : phương trình vô nghiệm thực
* - delta = 0  : phương trình có nghiệm kép
* - delta > 0  : phương trình có 2 nghiệm phân biệt
*
* Mã giả:
* 1. Nhập a, b, c
* 2. Nếu a = 0 thì thông báo không phải phương trình bậc 2
* 3. Ngược lại tính delta = b^2 - 4*a*c
* 4. Nếu delta < 0 thì báo vô nghiệm thực
* 5. Nếu delta = 0 thì tính x = -b / (2*a)
* 6. Nếu delta > 0 thì:
*    - x1 = (-b + sqrt(delta)) / (2*a)
*    - x2 = (-b - sqrt(delta)) / (2*a)
* 7. In nghiệm của phương trình
*/
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Enter a: ");
        double a = double.Parse(Console.ReadLine());

        Console.WriteLine("Enter b: ");
        double b = double.Parse(Console.ReadLine());

        Console.WriteLine("Enter c: ");
        double c = double.Parse(Console.ReadLine());


        //check
        if(a == 0)
        {
                Console.WriteLine("Not a equadratic equation");
        } else
        {
            double delta = b * b - 4*a*c;
            if(delta < 0)
            {
                Console.WriteLine("No real solution");
            } else if (delta == 0)
            {
                Console.WriteLine($"The equation has a double root: x = {(-(double)b / (2 * a)):F2}");
            } else
            {
                Console.WriteLine($"""
                The equation has 2 distinct roots:
                x1 = {((double)(-b + Math.Sqrt(delta)) / (2 * a)):F2}
                x2 = {((double)(-b - Math.Sqrt(delta)) / (2 * a)):F2}
                """);
            }
        }
    }
}