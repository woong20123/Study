#include <iostream>
#include <functional>
#include "TGDAllocate.hpp"
#include "ElapsedTimeOutput.h"

struct Data {
	char buffer[100];
	int value[32];
};

void TestRun(size_t threadCount, std::function<void()> TestLogic) 
{
	std::vector<std::thread> thread_group_;
	thread_group_.reserve(100);

	for (size_t tc = 0; tc < threadCount; tc++)
	{
		thread_group_.emplace_back(TestLogic);
	}

	for (auto& t : thread_group_) {
		t.join();
	}
}

int main() {
	
	try{
		size_t testSize = 100000;
		size_t threadCount = 10;

		std::cout << "[Test Environment]" << std::endl;
		std::cout << "Test ObjCount = " << testSize << " threadCount = " << threadCount << std::endl;
		std::shared_ptr<TGD::TGDAllocate<Data>> alloc = std::make_shared<TGD::TGDAllocate<Data>>();
		// TGD allocate Test
		{
			
			ElapsedTimeOutput TGDAllocatorElapsed("TGD Allocator");
			TestRun(threadCount, [alloc, testSize]() {
				for (size_t i = 0; i < testSize; i++) {

					auto pi = alloc->Allocate();
					//pi->value[0] = (int)i;
					if (i % 2 == 0)
						alloc->DeAllocate(pi);

				}
				});
			TGDAllocatorElapsed.milliSecPrint();
			
		}

		// new allocate Test
		{
			std::vector<std::thread> thread_group_;
			ElapsedTimeOutput Elapsed("new");

			TestRun(threadCount, [testSize]() {

				for (size_t i = 0; i < testSize; i++) {

					auto pi = new Data;
					//pi->value[0] = (int)i;
					if (i % 2 == 0)
						delete pi;

				}
				});
			Elapsed.milliSecPrint();
		}

	}
	catch (std::exception e) {
		std::cout << e.what() << std::endl;
	}
}