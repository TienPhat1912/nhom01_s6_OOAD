/*
 * CHƯƠNG TRÌNH TÍNH TỔNG ĐOẠN
 * Tác giả  : Nguyễn Khải Hoàn Ca
 * Ngày viết: 8/9/2026
 *
 * Phát biểu đề bài:
 * Nhập vào hai số nguyên a và b.
 * Tính tổng các số nguyên từ a đến b.
 * 
 * Ý tưởng: 
 * Gán biến tạm tmp = a.
 * Dùng vòng lặp while để cộng lần lượt các giá trị
 * từ a đến b vào biến result.
 *
 * Mã giả: 
 * Bắt đầu:
 * 1. Nhập a, b
 * 2. Gán result = 0
 * 3. Gán tmp = a
 * 4. While tmp <= b:
 *    - result = result + tmp
 *    - tmp = tmp + 1
 * 5. In result
 * Kết thúc
 */
 
Console.WriteLine("Enter a, b: ");

string[] input = Console.ReadLine().Split();

int a = int.Parse(input[0]);
int b = int.Parse(input[1]);

int result = 0;

int tmp = a;
while (tmp <= b)
{
    result += tmp;
    tmp++;
}

Console.WriteLine("Sum of [" + a + " -> " + b + "] is: " + result);