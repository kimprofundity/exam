// 使用者相關類型
export interface User {
  id: string
  username: string
  name: string
  email: string
  roles: string[]
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

// 員工相關類型
export interface Employee {
  id: string
  employeeNumber: string
  name: string
  departmentId: string
  position: string
  monthlySalary: number
  bankCode: string
  bankAccount: string
  status: 'Active' | 'Resigned'
  resignationDate?: string
}

// 薪資記錄類型
export interface SalaryRecord {
  id: string
  employeeId: string
  period: string
  baseSalary: number
  totalAdditions: number
  totalDeductions: number
  grossSalary: number
  netSalary: number
  status: 'Draft' | 'Approved' | 'Paid'
}

// API 回應類型
export interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

export interface PaginatedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
