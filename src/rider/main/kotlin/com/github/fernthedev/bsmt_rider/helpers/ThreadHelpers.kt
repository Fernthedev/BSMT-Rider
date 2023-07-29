package com.github.fernthedev.bsmt_rider.helpers

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.invokeLater
import com.intellij.openapi.application.runReadAction
import com.intellij.openapi.application.runWriteAction
import com.intellij.openapi.progress.ProgressManager

// https://plugins.jetbrains.com/docs/intellij/general-threading-rules.html#read-action-cancellability
fun <V : Any?> runReadActionSafely(runnable: () -> V): V {
    val canRead =
        ApplicationManager.getApplication().isReadAccessAllowed || ApplicationManager.getApplication().isDispatchThread
    if (canRead) {
        return runReadAction(runnable)
    }


    var late: V? = null
    while (!ProgressManager.getInstance().runInReadActionWithWriteActionPriority({
            ProgressManager.checkCanceled()

            late = runnable()

        }, null)) {
        // Avoid using resources
        Thread.yield()
        Thread.sleep(10)
    }

    return late as V
}

fun runWriteActionSafely(runnable: () -> Unit) {
    if (ApplicationManager.getApplication().isWriteAccessAllowed) {
        runnable();
    }

    if (ApplicationManager.getApplication().isDispatchThread) {
        runWriteAction(runnable);
    }

    invokeLater {
        runWriteAction(runnable)
    }
}

