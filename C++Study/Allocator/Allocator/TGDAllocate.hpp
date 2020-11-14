#include <shared_mutex>
#include <vector>
#include <map>
#include "TGDAllocatorImpl.h"

namespace TGD {
	template<typename T>
	class TGDAllocate {
	public:		
		using AllocateContainer = std::map<std::thread::id, std::shared_ptr<TGDAllocatorImpl>>;
		TGDAllocate();
		virtual ~TGDAllocate() {};

		T* Allocate();
		void DeAllocate(T*& ptr);

	protected:
		std::shared_ptr<TGDAllocatorImpl> GetAllocateImpl();

	private:
		AllocateContainer AllocateList;
		std::shared_ptr<TGDAllocatorImpl> allocateImpl;
		std::shared_mutex sm;
	};

	template<typename T>
	TGDAllocate<T>::TGDAllocate() 
	{
		allocateImpl = std::make_shared<TGDAllocatorImpl>(sizeof(T));
		AllocateList.insert(AllocateContainer::value_type(std::this_thread::get_id(), std::make_shared<TGDAllocatorImpl>(sizeof(T))));
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
	std::shared_ptr<TGDAllocatorImpl> TGDAllocate<T>::GetAllocateImpl()
	{
		const auto threadId = std::this_thread::get_id();
		auto itr = AllocateList.find(threadId);
		if (itr != AllocateList.end()) {
			return itr->second;
		}

		{
			std::unique_lock<std::shared_mutex> l(sm);
			auto newallocateImpl = std::make_shared<TGDAllocatorImpl>(sizeof(T));
			AllocateList.insert(AllocateContainer::value_type(threadId, newallocateImpl));
			return newallocateImpl;
		}
		
	}
}