#include <iostream>
#include <thread>
#include <atomic>

struct Point { int x, y; };             // 64비트 저장소
struct Point3D { int x, y, z; };        // 96비트 저장소를 가짐

std::atomic<int >       at1;
std::atomic<Point>      at2;            // at2.load(), at2.store()
std::atomic<Point3D>    at3;            

int main()
{
    ++at1;  // lock xadd ...

    std::cout << at1.is_lock_free() << std::endl;  // 1
    std::cout << at2.is_lock_free() << std::endl;  // 1
    std::cout << at3.is_lock_free() << std::endl;  // 0
}