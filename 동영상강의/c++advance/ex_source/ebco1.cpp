#include <iostream>

struct Empty {};

struct Data1
{
    Empty e;
    int data;
};

struct Data2 : public Empty
{
    int data;
};

int main(){
    std::cout << sizeof(Data1) << std::endl;
    std::cout << sizeof(Data2) << std::endl;
}
