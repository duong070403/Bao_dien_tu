// wwwroot/js/admin/layout.js

function initializeDropdown() {
  console.log("Dropdown initialized");
  $('.dropdown-toggle').dropdown();
}

function logoutUser() {
  $.ajax({
    type: "POST",
    url: "/api/Auth/logout",
    headers: {
      "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
    },
    success: function (response) {
      if (response.success) {
        window.location.href = "/";
      } else {
        alert("Đăng xuất thất bại: " + (response.message || "Lỗi không xác định"));
      }
    },
    error: function (xhr) {
      alert("Có lỗi xảy ra khi đăng xuất: " + (xhr.responseJSON?.message || xhr.statusText));
    }
  });
}

$(function () {
  initializeDropdown();
    $("#loginForm").on('submit', function (e) {
    e.preventDefault();
    $("#loginErrorMessage").hide();
    $(this).find('button[type="submit"]').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');
    $(this).find('button[type="submit"]').prop('disabled', true);

    var formData = new FormData();
    formData.append('Email', $("#loginEmail").val());
    formData.append('Password', $("#loginPassword").val());
    formData.append('RememberMe', $("#rememberMe").is(":checked"));

    $.ajax({
      type: "POST",
      url: "/api/Auth/login",
      data: formData,
      processData: false,
      contentType: false,
      cache: false,
      success: function (response) {
        if (response.success) {
          window.location.reload();
        } else {
          $("#loginErrorMessage").text(response.message).show();
          $("#loginForm").find('button[type="submit"]').html('<i class="fas fa-arrow-right me-2"></i> Đăng nhập');
          $("#loginForm").find('button[type="submit"]').prop('disabled', false);
        }
      },
      error: function (xhr) {
        var errorMsg = "Có lỗi xảy ra. Vui lòng thử lại sau.";
        if (xhr.responseJSON && xhr.responseJSON.message) {
          errorMsg = xhr.responseJSON.message;
        }
        $("#loginErrorMessage").text(errorMsg).show();
        $("#loginForm").find('button[type="submit"]').html('<i class="fas fa-arrow-right me-2"></i> Đăng nhập');
        $("#loginForm").find('button[type="submit"]').prop('disabled', false);
      }
    });
  });

    $("#registerForm").on('submit', function (e) {
    e.preventDefault();
    $("#registerErrorMessage").hide();
    $(this).find('button[type="submit"]').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');
    $(this).find('button[type="submit"]').prop('disabled', true);

    var password = $("#registerPassword").val();
    var passwordRegex = /^(?=.*[A-Z])(?=.*\d).{6,8}$/;
    if (!passwordRegex.test(password)) {
      $("#registerErrorMessage").text("Mật khẩu phải từ 6-8 ký tự, chứa ít nhất một chữ cái in hoa và một số.").show();
      $(this).find('button[type="submit"]').html('<i class="fas fa-user-check me-2"></i> Đăng ký');
      $(this).find('button[type="submit"]').prop('disabled', false);
      return;
    }

    if ($("#registerPassword").val() !== $("#confirmPassword").val()) {
      $("#registerErrorMessage").text("Mật khẩu xác nhận không khớp.").show();
      $(this).find('button[type="submit"]').html('<i class="fas fa-user-check me-2"></i> Đăng ký');
      $(this).find('button[type="submit"]').prop('disabled', false);
      return;
    }

    var formData = new FormData();
    formData.append('FullName', $("#registerFullName").val());
    formData.append('Email', $("#registerEmail").val());
    formData.append('Password', $("#registerPassword").val());
    formData.append('ConfirmPassword', $("#confirmPassword").val());

    $.ajax({
      type: "POST",
      url: "/api/Auth/register",
      data: formData,
      processData: false,
      contentType: false,
      cache: false,
      success: function (response) {
        if (response.success) {
          $('#successMessage').text(response.message);
          $('#successModal').modal('show');
          setTimeout(function () {
            $('#successModal').modal('hide'); // Ẩn modal thành công sau 2 giây
            $('#registerModal').modal('hide'); // Ẩn modal đăng ký
            $('#loginModal').modal('show'); // Hiển thị modal đăng nhập
          }, 2000);
          $("#registerForm")[0].reset(); // Reset form đăng ký
        } else {
          $("#registerErrorMessage").text(response.message).show();
          $(this).find('button[type="submit"]').html('<i class="fas fa-user-check me-2"></i> Đăng ký');
          $(this).find('button[type="submit"]').prop('disabled', false);
        }
      },
      error: function (xhr) {
        var errorMsg = xhr.responseJSON?.message || "Có lỗi xảy ra. Vui lòng thử lại sau.";
        $("#registerErrorMessage").text(errorMsg).show();
        $("#registerForm").find('button[type="submit"]').html('<i class="fas fa-user-check me-2"></i> Đăng ký');
        $("#registerForm").find('button[type="submit"]').prop('disabled', false);
      }
    });
  });

  // Add this to your existing JavaScript, inside the document ready function:
  var lastNotificationCount = 0;

  function loadNotifications() {
    console.log("Bắt đầu tải thông báo...");
    $.ajax({
      type: "GET",
      url: "/Sharing/GetNotifications",
      success: function (response) {
        console.log("Kết quả API notifications:", response);
        if (response.success) {
          var notifications = response.notifications || [];

          // Sort notifications: unread first, then by date
          notifications.sort(function (a, b) {
            if (a.isRead !== b.isRead) {
              return a.isRead ? 1 : -1;
            }
            return new Date(b.shareDate) - new Date(a.shareDate);
          });

          var unreadCount = notifications.filter(n => !n.isRead).length;
          var $notificationBadge = $('#notification-badge');
          var $notificationContainer = $('#notification-container');
          var $noNotifications = $('#no-notifications');
          var $bellIcon = $('.fas.fa-bell');

          // Trigger bell animation if unread count increases
          if (unreadCount > lastNotificationCount && lastNotificationCount !== 0) {
            $bellIcon.removeClass('bell-animate'); // Reset animation
            void $bellIcon[0].offsetWidth; // Force reflow to restart animation
            $bellIcon.addClass('bell-animate'); // Apply animation
          }

          lastNotificationCount = unreadCount;

          // Update badge visibility
          if (unreadCount > 0) {
            $notificationBadge.text(unreadCount).show();
            $noNotifications.hide();
          } else {
            $notificationBadge.hide();
            if (notifications.length === 0) {
              $noNotifications.show();
            } else {
              $noNotifications.hide();
            }
          }

          $notificationContainer.find('.notification-item').remove();

          if (notifications.length > 0) {
            notifications.forEach(function (notification) {
              var date = new Date(notification.shareDate);
              var formattedDate = new Intl.DateTimeFormat('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
              }).format(date);

              var itemClass = notification.isRead ? '' : 'unread';
              var messageHtml = notification.message ?
                `<small class="text-muted">"${notification.message}"</small><br>` : '';

              var notificationHtml = `
                                <a href="/News/Read/${notification.newsId}" class="dropdown-item notification-item ${itemClass}" data-id="${notification.id}">
                                    <div class="d-flex w-100 justify-content-between">
                                        <h6 class="mb-1 text-truncate">${notification.title}</h6>
                                        <small>${formattedDate}</small>
                                    </div>
                                    ${messageHtml}
                                    <small class="text-primary">Nhấn để xem bài viết</small>
                                </a>
                            `;
              $notificationContainer.append(notificationHtml);
            });
            $noNotifications.hide();
          } else {
            $noNotifications.show();
          }

          $('.notification-item').on('click', function () {
            var notificationId = $(this).data('id');
            markNotificationAsRead(notificationId);
          });
        } else {
          console.error("Lỗi khi tải thông báo:", response.message);
        }
      },
      error: function (xhr, status, error) {
        console.error("AJAX error:", status, error);
        console.error("Response:", xhr.responseText);
      }
    });
  }

  function markNotificationAsRead(notificationId) {
    $.ajax({
      type: "POST",
      url: "/Sharing/MarkAsRead",
      data: { notificationId: notificationId },
      headers: {
        "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
      },
      success: function (response) {
        if (response.success) {
          console.log("Đã đánh dấu thông báo đã đọc:", notificationId);
        }
      },
      error: function (xhr) {
        console.error("Lỗi khi đánh dấu thông báo là đã đọc:", xhr.responseText);
      }
    });
  }

    $("#markAllAsRead").on('click', function (e) {
    e.preventDefault();
    e.stopPropagation();
    $.ajax({
      type: "POST",
      url: "/Sharing/MarkAllAsRead",
      headers: {
        "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
      },
      success: function (response) {
        if (response.success) {
          loadNotifications();
        }
      }
    });
  });

  if ($('#notificationDropdown').length) {
    loadNotifications();
    setInterval(loadNotifications, 30000);
  }
});

document.addEventListener('DOMContentLoaded', function () {
    // Get current URL path
    const path = window.location.pathname;
    const categoryId = getCategoryIdFromPath(path);

    // Find all category navigation links
    const categoryLinks = document.querySelectorAll('.navbar-nav .nav-link');

    // Remove any existing active classes
    categoryLinks.forEach(link => {
        link.classList.remove('active');
    });

    // Add active class to current category
    if (categoryId) {
        const activeLink = document.querySelector(`.nav-link[href*="Category/${categoryId}"]`);
        if (activeLink) {
            activeLink.classList.add('active');
        }
    }

    // Handle Home page
    if (path === '/' || path.toLowerCase() === '/home' || path.toLowerCase() === '/home/index') {
        const homeLink = document.querySelector('.navbar-brand');
        if (homeLink) {
            homeLink.classList.add('active-home');
        }
    }
});

// Extract category ID from URL
function getCategoryIdFromPath(path) {
    if (path.includes('/Home/Category/')) {
        const parts = path.split('/');
        return parts[parts.length - 1];
    }
    return null;
}

// Handle Change Password
$("#changePasswordForm").on('submit', function (e) {
    e.preventDefault();
    $("#passwordErrorMessage").hide();
    $(this).find('button[type="submit"]').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');
    $(this).find('button[type="submit"]').prop('disabled', true);

    var currentPassword = $("#currentPassword").val();
    var newPassword = $("#newPassword").val();
    var confirmPassword = $("#confirmNewPassword").val();

    // Validate new password format
    var passwordRegex = /^(?=.*[A-Z])(?=.*\d).{6,8}$/;
    if (!passwordRegex.test(newPassword)) {
        $("#passwordErrorMessage").text("Mật khẩu phải từ 6-8 ký tự, chứa ít nhất một chữ cái in hoa và một số.").show();
        $(this).find('button[type="submit"]').html('<i class="fas fa-save me-2"></i> Cập nhật mật khẩu');
        $(this).find('button[type="submit"]').prop('disabled', false);
        return;
    }

    // Check if passwords match
    if (newPassword !== confirmPassword) {
        $("#passwordErrorMessage").text("Xác nhận mật khẩu không khớp.").show();
        $(this).find('button[type="submit"]').html('<i class="fas fa-save me-2"></i> Cập nhật mật khẩu');
        $(this).find('button[type="submit"]').prop('disabled', false);
        return;
    }

    var formData = new FormData();
    formData.append('CurrentPassword', currentPassword);
    formData.append('NewPassword', newPassword);
    formData.append('ConfirmPassword', confirmPassword);

    $.ajax({
        type: "POST",
        url: "/api/Auth/changePassword",
        data: formData,
        processData: false,
        contentType: false,
        cache: false,
        headers: {
            "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                // Reset form
                $("#changePasswordForm")[0].reset();
                // Hide change password modal
                $('#changePasswordModal').modal('hide');
                // Show success message
                $('#successMessage').text(response.message);
                $('#successModal').modal('show');
            } else {
                $("#passwordErrorMessage").text(response.message).show();
                $("#changePasswordForm").find('button[type="submit"]').html('<i class="fas fa-save me-2"></i> Cập nhật mật khẩu');
                $("#changePasswordForm").find('button[type="submit"]').prop('disabled', false);
            }
        },
        error: function (xhr) {
            var errorMsg = xhr.responseJSON?.message || "Có lỗi xảy ra. Vui lòng thử lại sau.";
            $("#passwordErrorMessage").text(errorMsg).show();
            $("#changePasswordForm").find('button[type="submit"]').html('<i class="fas fa-save me-2"></i> Cập nhật mật khẩu');
            $("#changePasswordForm").find('button[type="submit"]').prop('disabled', false);
        }
    });
});

// Add this to the layout.js file to handle animated icons in modals

// Function to add animated icons to modals
function setupAnimatedModalTitles() {
    // Add the animated class to all modal titles with icons
    document.querySelectorAll('.modal-title i').forEach(icon => {
        icon.classList.add('icon-animated');
    });

    // Add specific animation to active modals
    $('.modal').on('show.bs.modal', function () {
        $(this).find('.modal-title').addClass('modal-title-animated');
    });

    $('.modal').on('hide.bs.modal', function () {
        $(this).find('.modal-title').removeClass('modal-title-animated');
    });
}

// Call this function on document ready
$(function () {
    // Existing code...

    // Setup animated modal titles
    setupAnimatedModalTitles();
});

// Update the Change Password handler to include animation
$("#changePasswordForm").on('submit', function (e) {
    e.preventDefault();
    $("#passwordErrorMessage").hide();

    // Add animation to the modal title icon
    $('#changePasswordModalLabel i').addClass('icon-spin');

    $(this).find('button[type="submit"]').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');
    $(this).find('button[type="submit"]').prop('disabled', true);

    // Rest of your code...

    $.ajax({
        // Existing AJAX call...
        success: function (response) {
            // Remove icon animation
            $('#changePasswordModalLabel i').removeClass('icon-spin');

            // Rest of your success handler...
        },
        error: function (xhr) {
            // Remove icon animation
            $('#changePasswordModalLabel i').removeClass('icon-spin');

            // Rest of your error handler...
        }
    });
});

document.addEventListener('DOMContentLoaded', function () {
    // Handle footer subscription form
    const footerSubscriptionForm = document.getElementById('footerSubscriptionForm');
    if (footerSubscriptionForm) {
        footerSubscriptionForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const email = document.getElementById('footerEmail').value.trim();
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const subscribeBtn = footerSubscriptionForm.querySelector('.btn-subscribe');
            const originalBtnHtml = subscribeBtn.innerHTML;

            // Show loading state
            subscribeBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
            subscribeBtn.disabled = true;

            // Submit the form via AJAX
            fetch('/News/Subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: `email=${encodeURIComponent(email)}&__RequestVerificationToken=${encodeURIComponent(token)}`
            })
                .then(response => response.json())
                .then(data => {
                    // Reset button state
                    subscribeBtn.innerHTML = originalBtnHtml;
                    subscribeBtn.disabled = false;

                    // Show result
                    const resultDiv = document.getElementById('subscriptionResult');
                    resultDiv.style.display = 'block';

                    if (data.success) {
                        resultDiv.className = 'mt-2 text-success';
                        resultDiv.innerHTML = `<small><i class="fas fa-check-circle me-1"></i>${data.message}</small>`;
                        footerSubscriptionForm.reset();
                    } else {
                        resultDiv.className = 'mt-2 text-warning';
                        resultDiv.innerHTML = `<small><i class="fas fa-exclamation-triangle me-1"></i>${data.message}</small>`;
                    }

                    // Hide result after 5 seconds
                    setTimeout(() => {
                        resultDiv.style.display = 'none';
                    }, 5000);
                })
                .catch(error => {
                    // Reset button state
                    subscribeBtn.innerHTML = originalBtnHtml;
                    subscribeBtn.disabled = false;

                    // Show error
                    const resultDiv = document.getElementById('subscriptionResult');
                    resultDiv.style.display = 'block';
                    resultDiv.className = 'mt-2 text-warning';
                    resultDiv.innerHTML = '<small><i class="fas fa-exclamation-triangle me-1"></i>Có lỗi xảy ra. Vui lòng thử lại sau.</small>';

                    console.error('Subscription error:', error);
                });
        });
    }
});