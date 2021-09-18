#include <chrono>
#include <iostream>
#include <thread>
using namespace std::literals;

int main()
{
    std::jthread t(foo, 10, 3.4)
}