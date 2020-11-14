#include <vector>
#include <set>
#include <deque>
#include <shared_mutex>
#include <Windows.h>
#include "TGDAllocatorImpl.h"

namespace TGD {
	struct TGDAllocatorImpl::Impl {
		void* chunk;
		size_t chunkSize;
		size_t chunckAllocIndex;
		size_t objectSize;
		size_t objectPerChunk;		
		std::vector<void*> waitingMemory;
		std::vector<void*> chuckLists;
	};

	TGDAllocatorImpl::TGDAllocatorImpl(size_t objectSize)
	{
		pimpl = std::make_shared<Impl>();
		pimpl->chunk = nullptr;
		pimpl->chunkSize = 0;
		pimpl->chunckAllocIndex = 0;
		pimpl->objectSize = objectSize;
		pimpl->objectPerChunk = 1024;
		pimpl->waitingMemory.reserve(1024);
	}

	TGDAllocatorImpl::~TGDAllocatorImpl() 
	{
		for (auto& pchuck : pimpl->chuckLists) 
		{
			//free(pchuck);
			::VirtualFree(pchuck, 0, MEM_RELEASE);
		}
	}

	bool TGDAllocatorImpl::Allocate(void *& allocate) 
	{
		if (pimpl->waitingMemory.size() > 0)
		{
			allocate = *pimpl->waitingMemory.rbegin();
			pimpl->waitingMemory.pop_back();
			return true;
		}
		
		if (CheckNewChuck()) {
			if (false == newChunk())
			{
				allocate = nullptr;
			}
		}

		{
			allocate = static_cast<void*>(static_cast<char*>(pimpl->chunk) + pimpl->chunckAllocIndex);
			pimpl->chunckAllocIndex += pimpl->objectSize;
		}
		return true;
	}

	void TGDAllocatorImpl::DeAllocate(void*  ptr)
	{
		pimpl->waitingMemory.push_back(ptr);
	}


	bool TGDAllocatorImpl::CheckNewChuck() {
		if (!pimpl->chunk)
			return true;

		if (pimpl->chunkSize <= pimpl->chunckAllocIndex + pimpl->objectSize)
			return true;

		return false;
	}

	bool TGDAllocatorImpl::newChunk() {

		size_t allocPageSize = ((pimpl->objectSize * pimpl->objectPerChunk) / 4096) + 1;

		pimpl->chunk = VirtualAlloc(NULL, allocPageSize * 4096, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
		pimpl->chuckLists.push_back(pimpl->chunk);
		pimpl->chunkSize = allocPageSize * 4096;
		pimpl->chunckAllocIndex = 0;

		// set new objectPerChunk value
		pimpl->objectPerChunk = pimpl->objectPerChunk + (pimpl->objectPerChunk / 2);
		return true;
	}
}