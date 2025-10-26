"use client";
import { toast, ToastContainer } from "react-toastify";

export default function Home() {
  const notify = () => toast("Wow so easy!");
  return (
    <div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans">
      <main className="flex min-h-screen w-full max-w-3xl flex-col items-center justify-between py-32 px-16 bg-white dark:bg-black sm:items-start">
        <button onClick={notify}>Notify!</button>
        <ToastContainer />
      </main>
    </div>
  );
}
