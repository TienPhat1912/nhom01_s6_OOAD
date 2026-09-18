/*
* CHƯƠNG TRÌNH PHÂN LOẠI TAM GIÁC
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 10/9/2026
*
* Phát biểu đề bài:
* Nhập vào ba số thực a, b, c là độ dài ba cạnh.
* Kiểm tra ba cạnh có tạo thành tam giác hay không.
* Nếu có, xác định loại tam giác:
* tam giác đều, vuông cân, vuông, cân hoặc thường.
*
* Ý tưởng:
* Sắp xếp ba cạnh theo thứ tự tăng dần để:
*   a <= b <= c
*
* Kiểm tra điều kiện tạo thành tam giác:
*   a > 0 và a + b > c
*
* Sau đó xác định:
* - Tam giác đều: a = b = c
* - Tam giác cân: có ít nhất hai cạnh bằng nhau
* - Tam giác vuông: a^2 + b^2 = c^2
* - Tam giác vuông cân: vừa vuông vừa cân
* - Nếu không thuộc các trường hợp trên thì là tam giác thường
*
* Mã giả:
* 1. Nhập a, b, c
* 2. Sắp xếp a, b, c theo thứ tự tăng dần
* 3. Nếu a <= 0 hoặc a + b <= c:
*    - Thông báo không tạo thành tam giác
* 4. Ngược lại:
*    - Kiểm tra tam giác đều
*    - Kiểm tra tam giác cân
*    - Kiểm tra tam giác vuông
* 5. Nếu đều thì in "Tam giác đều"
* 6. Ngược lại nếu vừa vuông vừa cân thì in "Tam giác vuông cân"
* 7. Ngược lại nếu vuông thì in "Tam giác vuông"
* 8. Ngược lại nếu cân thì in "Tam giác cân"
* 9. Ngược lại in "Tam giác thường"
*/

using System;

class Program
{
    static void Main()
    {
        Console.Write("Enter a, b, c: ");
        string[] input = Console.ReadLine().Split();

        double a = double.Parse(input[0]);
        double b = double.Parse(input[1]);
        double c = double.Parse(input[2]);
        double[] sides = { a, b, c };
        Array.Sort(sides);

        a = sides[0];
        b = sides[1];
        c = sides[2];

        if (a <= 0 || a + b <= c)
        {
            Console.WriteLine("These sides do not form a triangle.");
        }
        else
        {
            bool isEquilateral = a == b && b == c;
            bool isIsosceles = a == b || b == c || a == c;
            bool isRight = a * a + b * b == c * c;

            if (isEquilateral)
            {
                Console.WriteLine("This is an equilateral triangle.");
            }
            else if (isRight && isIsosceles)
            {
                Console.WriteLine("This is a right isosceles triangle.");
            }
            else if (isRight)
            {
                Console.WriteLine("This is a right triangle.");
            }
            else if (isIsosceles)
            {
                Console.WriteLine("This is an isosceles triangle.");
            }
            else
            {
                Console.WriteLine("This is a scalene triangle.");
            }
        }
    }
}