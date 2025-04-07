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
