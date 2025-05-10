"use client"

import * as React from "react"
import { cn } from "@/lib/utils"

interface StepsProps extends React.HTMLAttributes<HTMLDivElement> {
  currentStep: number
}

export function Steps({ currentStep, className, ...props }: StepsProps) {
  const childrenArray = React.Children.toArray(props.children)
  const totalSteps = childrenArray.length

  return (
    <div className={cn("flex w-full", className)} {...props}>
      <StepsProvider currentStep={currentStep} totalSteps={totalSteps}>
        {props.children}
      </StepsProvider>
    </div>
  )
}

interface StepProps extends React.HTMLAttributes<HTMLDivElement> {
  title: string
  description?: string
}

export function Step({ title, description, className, ...props }: StepProps) {
  const stepContext = React.useContext(StepContext)
  const index = React.useRef(stepContext.index).current
  stepContext.index += 1

  const status =
    stepContext.currentStep === index ? "current" : stepContext.currentStep > index ? "complete" : "incomplete"

  return (
    <div className={cn("flex-1 relative", index !== 0 && "ml-6", className)} {...props}>
      <div className="flex items-center">
        <div
          className={cn(
            "flex items-center justify-center w-8 h-8 rounded-full border-2 z-10",
            status === "complete" && "bg-purple-600 border-purple-600 text-white",
            status === "current" && "border-purple-600 text-purple-600",
            status === "incomplete" && "border-gray-300 text-gray-300",
          )}
        >
          {status === "complete" ? (
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          ) : (
            index + 1
          )}
        </div>
        {index !== stepContext.totalSteps - 1 && (
          <div
            className={cn(
              "flex-1 h-0.5 ml-2",
              status === "complete" && "bg-purple-600",
              (status === "current" || status === "incomplete") && "bg-gray-300",
            )}
          />
        )}
      </div>
      <div className="mt-2">
        <div
          className={cn(
            "text-sm font-medium",
            status === "complete" && "text-gray-900 dark:text-gray-100",
            status === "current" && "text-purple-600",
            status === "incomplete" && "text-gray-500",
          )}
        >
          {title}
        </div>
        {description && (
          <div
            className={cn(
              "text-xs",
              status === "complete" && "text-gray-600 dark:text-gray-400",
              status === "current" && "text-purple-600",
              status === "incomplete" && "text-gray-500",
            )}
          >
            {description}
          </div>
        )}
      </div>
    </div>
  )
}

interface StepContextType {
  currentStep: number
  index: number
  totalSteps: number
}

const StepContext = React.createContext<StepContextType>({
  currentStep: 0,
  index: 0,
  totalSteps: 0,
})

export function StepsProvider({
  currentStep,
  totalSteps,
  children,
}: {
  currentStep: number
  totalSteps: number
  children: React.ReactNode
}) {
  return <StepContext.Provider value={{ currentStep, index: 0, totalSteps }}>{children}</StepContext.Provider>
}

export { StepContext }
