#include <iostream>
#include <thread>
#include <chrono>
using namespace std::literals;

int main() {
    std::chrono::hours          h(10);
    std::chrono::minutes        m(10);
    std::chrono::seconds        s(10);
    std::chrono::milliseconds   ms(10);
    std::chrono::nanoseconds    ns(10);

    std::cout << s.count() << std::endl;

    std::chrono::system_clock::time_point tp1 = std::chrono::system_clock::now();
    std::chrono::hours h2 = std::chrono::duration_cast< std::chrono::hours>(tp1.time_since_epoch());
    std::cout << h2.count() << std::endl;
}