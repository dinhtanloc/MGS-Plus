import { defineStore } from 'pinia'
import { ref } from 'vue'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface Toast {
  id: number
  type: ToastType
  message: string
  duration: number
}

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<Toast[]>([])
  let nextId = 1

  function notify(type: ToastType, message: string, duration = 3500) {
    const id = nextId++
    toasts.value.push({ id, type, message, duration })
    setTimeout(() => remove(id), duration)
  }

  function success(message: string, duration?: number) { notify('success', message, duration) }
  function error(message: string, duration?: number)   { notify('error',   message, duration) }
  function warning(message: string, duration?: number) { notify('warning', message, duration) }
  function info(message: string, duration?: number)    { notify('info',    message, duration) }

  function remove(id: number) {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  return { toasts, notify, success, error, warning, info, remove }
})
