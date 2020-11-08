#include <shared_mutex>
#include <vector>
#include <unordered_map>
#include "TGDAllocatorImpl.h"

namespace TGD {
	template<typename T>
	class TGDAllocate {
	public:		
		using AllocateContainer = std::unordered_map<std::thread::id, std::unique_ptr<TGDAllocatorImpl>>;
		TGDAllocate();
		virtual ~TGDAllocate() {};

		T* Allocate();
		void DeAllocate(T*& ptr);

	protected:
		TGDAllocatorImpl* GetAllocateImpl();

	private:
		std::unordered_map<std::thread::id, std::unique_ptr<TGDAllocatorImpl>> AllocateList;
		std::unique_ptr<TGDAllocatorImpl> allocateImpl;
		std::shared_mutex sm;
	};

	template<typename T>
	TGDAllocate<T>::TGDAllocate() 
	{
		allocateImpl = std::make_unique<TGDAllocatorImpl>(sizeof(T));
	}

	template<typename T>
	T* TGDAllocate<T>::Allocate() {
		void* alloc = nullptr;
		{
			//std::unique_lock<std::shared_mutex> l(sm);
			if (!alloc)
			{
				GetAllocateImpl()->Allocate(alloc);
			}
		}
		return new (alloc) T;
	}

	template<typename T>
	void TGDAllocate<T>::DeAllocate(T*& ptr) {
		{
			//std::unique_lock<std::shared_mutex> l(sm);
			ptr->~T();
			GetAllocateImpl()->DeAllocate(ptr);
		}
	}

	template<typename T>
	TGDAllocatorImpl* TGDAllocate<T>::GetAllocateImpl() 
	{
		const auto threadId = std::this_thread::get_id();
		const auto itr = AllocateList.find(std::this_thread::get_id());
		if (itr == AllocateList.end())
		{
			return AllocateList.insert(AllocateContainer::value_type(threadId, std::make_unique<TGDAllocatorImpl>(sizeof(T)))).first->second.get();			
		}
		return itr->second.get();
	}
}