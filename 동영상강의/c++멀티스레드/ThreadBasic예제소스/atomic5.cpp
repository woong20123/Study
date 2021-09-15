#include <iostream>

#include <atomic>

struct Point { 
    int x, y; 
    Point() = default;

    //Point(const Point&) {}
};



std::atomic<Point> pt;


int main()
{
    Point ret = pt.load();
}