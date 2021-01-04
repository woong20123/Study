#include <shared_mutex>
#include <vector>
#include <map>
#include "TGDAllocatorImpl.h"

namespace TGD {
	template<typename T>
	class TGDAllocate {
	public:		
		using AllocateContainer = std::map<std::thread::id, TGDAllocatorImpl*>;
		TGDAllocate();
		~TGDAllocate();

		T* Allocate();
		void DeAllocate(T*& ptr);

	protected:
		TGDAllocatorImpl* GetAllocateImpl();

	private:
		AllocateContainer AllocateList;
		std::shared_mutex sm;
	};

	template<typename T>
	TGDAllocate<T>::TGDAllocate() 
	{
	}

	template<typename T>
	TGDAllocate<T>::~TGDAllocate()
	{
	
	}

	template<typename T>
	T* TGDAllocate<T>::Allocate() {
		void* alloc = nullptr;
		{
			if (!alloc)
			{
				GetAllocateImpl()->Allocate(alloc);
			}
		}
#pragma push_macro("new")
#undef new
		T* newObj = new (alloc) T;
#pragma pop_macro("new")
		return newObj;
	}

	template<typename T>
	void TGDAllocate<T>::DeAllocate(T*& ptr) {
		{
			ptr->~T();
			GetAllocateImpl()->DeAllocate(ptr);
		}
	}

	template<typename T>
	TGDAllocatorImpl * TGDAllocate<T>::GetAllocateImpl()
	{
		const auto threadId = std::this_thread::get_id();
		auto itr = AllocateList.find(threadId);
		if (itr != AllocateList.end()) {
			return itr->second;
		}

		{
			std::unique_lock<std::shared_mutex> l(sm);
			TGDAllocatorImpl * newallocateImpl = new TGDAllocatorImpl(sizeof(T));
			AllocateList.insert(AllocateContainer::value_type(threadId, newallocateImpl));
			auto itr = AllocateList.find(threadId);
			if (itr != AllocateList.end()) {
				return itr->second;
			}
		}

		return nullptr;
	}
}