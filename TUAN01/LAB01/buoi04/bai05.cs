/*
* CHƯƠNG TRÌNH KIỂM TRA TÍNH CHẤT SỐ
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 10/9/2026
*
* Phát biểu đề bài:
* Nhập vào một số tự nhiên n có 4 chữ số.
* Kiểm tra:
* - n có phải là số đối xứng hay không.
* - n có 3 chữ số giống nhau hay không.
* - n có 4 chữ số khác nhau hay không.
*
* Ý tưởng:
* Tách số n thành 4 chữ số a, b, c, d.
* Kiểm tra số đối xứng bằng cách so sánh:
*   a = d và b = c
*
* Sau đó đưa 4 chữ số vào mảng và sắp xếp tăng dần.
* Khi đã sắp xếp:
* - Nếu 3 phần tử liên tiếp bằng nhau thì có 3 chữ số giống nhau.
* - Nếu mọi cặp phần tử liên tiếp đều khác nhau thì có 4 chữ số khác nhau.
*
* Mã giả:
* 1. Nhập n
* 2. Tách:
*    - a = n / 1000
*    - b = (n / 100) % 10
*    - c = (n / 10) % 10
*    - d = n % 10
* 3. Nếu a = d và b = c:
*    - In "Số đối xứng"
*    Ngược lại:
*    - In "Không đối xứng"
* 4. Đưa a, b, c, d vào mảng digits
* 5. Sắp xếp digits tăng dần
* 6. Nếu digits[0] = digits[1] = digits[2]
*    hoặc digits[1] = digits[2] = digits[3]:
*    - In "Có 3 chữ số giống nhau"
*    Ngược lại:
*    - In "Không có 3 chữ số giống nhau"
* 7. Nếu digits[0] != digits[1]
*    và digits[1] != digits[2]
*    và digits[2] != digits[3]:
*    - In "Có 4 chữ số khác nhau"
*    Ngược lại:
*    - In "Không có 4 chữ số khác nhau"
*/

using System;

class Program
{
    static void Main()
    {
        Console.Write("Enter a 4-digit number: ");
        int n = int.Parse(Console.ReadLine());

        int a = n / 1000;
        int b = (n / 100) % 10;
        int c = (n / 10) % 10;
        int d = n % 10;

        // a. Check palindrome
        if (a == d && b == c)
        {
            Console.WriteLine($"{n} is a palindrome.");
        }
        else
        {
            Console.WriteLine($"{n} is not a palindrome.");
        }

        // Sort digits
        int[] digits = { a, b, c, d };
        Array.Sort(digits);

        // b. Check if there are 3 equal digits
        if ((digits[0] == digits[1] && digits[1] == digits[2]) ||
            (digits[1] == digits[2] && digits[2] == digits[3]))
        {
            Console.WriteLine($"{n} has 3 equal digits.");
        }
        else
        {
            Console.WriteLine($"{n} does not have 3 equal digits.");
        }

        // c. Check if all 4 digits are different
        if (digits[0] != digits[1] &&
            digits[1] != digits[2] &&
            digits[2] != digits[3])
        {
            Console.WriteLine($"{n} has 4 different digits.");
        }
        else
        {
            Console.WriteLine($"{n} does not have 4 different digits.");
        }
    }
}